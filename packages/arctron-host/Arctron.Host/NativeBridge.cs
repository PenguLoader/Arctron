using System.Diagnostics;

namespace Arctron.Host;

public sealed class NativeBridge
{
    private readonly IHostPlatform _platform;

    public NativeBridge(IHostPlatform platform)
    {
        _platform = platform;
    }

    public void AppQuit()
    {
        _platform.Quit();
    }

    public int WindowCreate(WindowOptions options)
    {
        return _platform.CreateWindow(options);
    }

    public void WindowLoadUrl(int id, string url)
    {
        _platform.LoadUrl(id, url);
    }

    public void WindowShow(int id)
    {
        _platform.ShowWindow(id);
    }

    public void WindowHide(int id)
    {
        _platform.HideWindow(id);
    }

    public void WindowClose(int id)
    {
        _platform.CloseWindow(id);
    }

    public void WindowSetTitle(int id, string title)
    {
        _platform.SetTitle(id, title);
    }

    public int[] WindowGetSize(int id)
    {
        return _platform.GetWindowSize(id);
    }

    public int[] WindowGetPosition(int id)
    {
        return _platform.GetWindowPosition(id);
    }

    public void WindowFocus(int id)
    {
        _platform.FocusWindow(id);
    }

    public void WindowCenter(int id)
    {
        _platform.CenterWindow(id);
    }

    public void WindowMinimize(int id)
    {
        _platform.MinimizeWindow(id);
    }

    public void WindowUnminimize(int id)
    {
        _platform.UnminimizeWindow(id);
    }

    public bool WindowIsMinimized(int id)
    {
        return _platform.IsWindowMinimized(id);
    }

    public void WindowMaximize(int id)
    {
        _platform.MaximizeWindow(id);
    }

    public void WindowUnmaximize(int id)
    {
        _platform.UnmaximizeWindow(id);
    }

    public bool WindowIsMaximized(int id)
    {
        return _platform.IsWindowMaximized(id);
    }

    public void WindowSetRpcManifest(int id, string? rpcNamespace, string[] methods)
    {
        _platform.SetWindowRpcManifest(id, rpcNamespace, methods);
    }

    public void WindowOnRpcInvoke(Action<int, string, string, string> handler)
    {
        _platform.SetWindowRpcInvokeSink(handler);
    }

    public void WindowRpcResolve(int id, string requestId, string resultJson)
    {
        _platform.ResolveWindowRpc(id, requestId, resultJson);
    }

    public void WindowRpcReject(int id, string requestId, string error)
    {
        _platform.RejectWindowRpc(id, requestId, error);
    }

    public int TrayCreate(TrayOptions options)
    {
        return _platform.CreateTray(options);
    }

    public void TraySetToolTip(int id, string tooltip)
    {
        _platform.SetTrayToolTip(id, tooltip);
    }

    public void TraySetMenu(int id, TrayMenuItem[] menu)
    {
        _platform.SetTrayMenu(id, menu);
    }

    public void TrayOnEvent(Action<int, string, string?> handler)
    {
        _platform.SetTrayEventSink(handler);
    }

    public string[] DialogOpenFile(OpenDialogOptions options)
    {
        return _platform.OpenFileDialog(options);
    }

    public string? DialogSaveFile(SaveDialogOptions options)
    {
        return _platform.SaveFileDialog(options);
    }

    public MessageBoxResult DialogMessageBox(MessageBoxOptions options)
    {
        return _platform.ShowMessageBox(options);
    }

    public void ShellOpenExternal(string url)
    {
        _platform.OpenExternal(url);
    }

    public void ShellOpenPath(string path)
    {
        _platform.OpenPath(path);
    }

    public void ShellRevealPath(string path)
    {
        _platform.RevealPath(path);
    }

    public void ShellTrashPath(string path)
    {
        _platform.TrashPath(path);
    }

    public string PathJoin(string[] parts)
    {
        return _platform.JoinPath(parts);
    }

    public string PathResolve(string[] parts)
    {
        return _platform.ResolvePath(parts);
    }

    public string PathGetUserData()
    {
        return _platform.GetUserDataPath();
    }

    public string PathGetLocalData()
    {
        return _platform.GetLocalDataPath();
    }

    public string PathGetTemp()
    {
        return _platform.GetTempPath();
    }

    public string PathGetCwd()
    {
        return _platform.GetCurrentWorkingDirectory();
    }

    public string PathGetBaseExeDir()
    {
        return _platform.GetBaseExecutableDirectory();
    }

    public string FsReadText(string path)
    {
        return _platform.ReadTextFile(path);
    }

    public void FsWriteText(string path, string content)
    {
        _platform.WriteTextFile(path, content);
    }

    public void FsAppendText(string path, string content)
    {
        _platform.AppendTextFile(path, content);
    }

    public bool FsExists(string path)
    {
        return _platform.PathExists(path);
    }

    public void FsMkdir(string path, bool recursive)
    {
        _platform.CreateDirectory(path, recursive);
    }

    public string[] FsReadDir(string path)
    {
        return _platform.ReadDirectory(path);
    }

    public void FsRemove(string path, bool recursive)
    {
        _platform.RemovePath(path, recursive);
    }

    public ExecResult ProcessExec(string command, string[] args, ExecOptions options)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = options.Cwd ?? Environment.CurrentDirectory
        };

        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        if (options.Env != null)
        {
            foreach (var kvp in options.Env)
            {
                psi.Environment[kvp.Key] = kvp.Value;
            }
        }

        using var process = Process.Start(psi);
        if (process == null)
        {
            return new ExecResult { ExitCode = -1, Stderr = "Failed to start process" };
        }

        if (options.TimeoutMs.HasValue)
        {
            process.WaitForExit(options.TimeoutMs.Value);
        }
        else
        {
            process.WaitForExit();
        }

        return new ExecResult
        {
            ExitCode = process.ExitCode,
            Stdout = process.StandardOutput.ReadToEnd(),
            Stderr = process.StandardError.ReadToEnd()
        };
    }
}
