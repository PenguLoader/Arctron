using System.Collections.Concurrent;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.User32;

namespace Arctron.Host.Windows;

public sealed class MainThreadDispatcher : SynchronizationContext, IDisposable
{
    private readonly ConcurrentQueue<(SendOrPostCallback, object?)> _queue = new();
    private readonly AutoResetEvent _workItemsWaiting = new(false);

    private int _threadId;
    private bool _running;
    private Exception? _unhandledException;

    public void Post(Action action)
    {
        Post(_ => action(), null);
    }

    public void Invoke(Action action)
    {
        if (Thread.CurrentThread.ManagedThreadId == _threadId)
        {
            action();
            return;
        }
        using var done = new ManualResetEventSlim();
        Exception? captured = null;
        Post(s =>
        {
            try { action(); }
            catch (Exception ex) { captured = ex; }
            finally { done.Set(); }
        }, null);
        done.Wait();
        if (captured != null)
            throw captured;
    }

    public Task InvokeAsync(Action action)
    {
        if (Thread.CurrentThread.ManagedThreadId == _threadId)
        {
            try
            {
                action();
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                return Task.FromException(ex);
            }
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Post(() =>
        {
            try
            {
                action();
                tcs.SetResult();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        return tcs.Task;
    }

    public override void Post(SendOrPostCallback d, object? state)
    {
        _queue.Enqueue((d, state));
        _workItemsWaiting.Set(); // Wake the loop
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        if (Thread.CurrentThread.ManagedThreadId == _threadId)
        {
            d(state);
            return;
        }

        using var done = new ManualResetEventSlim();
        Exception? captured = null;

        Post(s =>
        {
            try { d(s); }
            catch (Exception ex) { captured = ex; }
            finally { done.Set(); }
        }, state);

        done.Wait();

        if (captured != null)
            throw captured;
    }

    public int Run(CancellationToken cancellationToken = default)
    {
        if (_running)
            throw new InvalidOperationException("Already running.");

        _threadId = Thread.CurrentThread.ManagedThreadId;
        SetSynchronizationContext(this);

        _running = true;

        MSG msg = default;
        HANDLE[] handles = [_workItemsWaiting.SafeWaitHandle.DangerousGetHandle()];

        try
        {
            while (_running && !cancellationToken.IsCancellationRequested)
            {
                // 1️. Process all pending Win32 messages
                while (PeekMessage(out msg, IntPtr.Zero, 0, 0, PM.PM_REMOVE))
                {
                    if (msg.message == (uint)WindowMessage.WM_QUIT)
                    {
                        _running = false;
                        break;
                    }

                    TranslateMessage(in msg);
                    DispatchMessage(in msg);
                }

                // 2️. Execute all queued work
                ProcessQueue();

                if (!_running)
                    break;

                // 3️. Wait efficiently for:
                //    - Win32 input
                //    - Or queued work event
                MsgWaitForMultipleObjectsEx(
                    1,
                    handles,
                    INFINITE,
                    QS.QS_ALLINPUT,
                    MWMO.MWMO_INPUTAVAILABLE);
            }

            ProcessQueue(); // final drain
        }
        catch (Exception ex)
        {
            _unhandledException = ex;
            throw;
        }
        finally
        {
            _running = false;
            SetSynchronizationContext(null);
        }

        if (_unhandledException != null)
            throw _unhandledException;

        return msg.wParam.ToInt32();
    }

    private void ProcessQueue()
    {
        while (_queue.TryDequeue(out var work))
        {
            try
            {
                work.Item1(work.Item2);
            }
            catch (Exception ex)
            {
                // Fail fast like real UI frameworks
                _unhandledException = ex;
                PostQuitMessage(-1);
                _running = false;
                break;
            }
        }
    }

    public void Exit()
    {
        if (Thread.CurrentThread.ManagedThreadId != _threadId)
        {
            if (_running)
            {
                Post(_ => Exit(), null);
            }
            return;
        }

        _running = false;
        _workItemsWaiting.Set();
        PostQuitMessage(0);
    }

    public void Dispose()
    {
        _workItemsWaiting.Dispose();
    }
}