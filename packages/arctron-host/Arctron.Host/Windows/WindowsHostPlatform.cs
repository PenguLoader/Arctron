using Arctron.Host.Windows;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace Arctron.Host;

[SupportedOSPlatform("windows")]
public sealed class WindowsHostPlatform : IHostPlatform
{
    private readonly object _sync = new();
    readonly MainThreadDispatcher _dispatcher = new();

    private readonly Dictionary<int, WindowRecord> _windows = new();
    private readonly Dictionary<nint, int> _hwndToId = new();
    private readonly AutoResetEvent _uiThreadReady = new(false);

    private int _nextWindowId = 1;
    private int _nextTrayId = 1;
    private ushort _windowClassAtom;
    private uint _uiThreadId;
    private Thread? _uiThread;
    private bool _running;
    private bool _quitRequested;

    private Action<int, string, string?>? _trayEventSink;
    private Action<int, string, string, string>? _windowRpcInvokeSink;

    private static readonly Native.WndProcDelegate WndProcDelegateInstance = WndProc;
    private static WindowsHostPlatform? s_current;

    public WindowsHostPlatform()
    {
        SynchronizationContext.SetSynchronizationContext(_dispatcher);
    }

    public int Run()
    {
        return _dispatcher.Run();
    }

    public void Quit()
    {
        _dispatcher.Exit();
    }

    public int CreateWindow(WindowOptions options)
    {
        EnsureUiStarted();

        var id = Interlocked.Increment(ref _nextWindowId) - 1;
        var record = new WindowRecord
        {
            Id = id,
            Width = options.Width ?? 1024,
            Height = options.Height ?? 768,
            X = options.X,
            Y = options.Y,
            Title = options.Title ?? "Arctron",
            Visible = options.Show ?? true,
            Frameless = options.Frameless ?? false,
            CurrentUrl = options.InitialUrl,
            DevTools = options.DevTools ?? false,
            ContextMenu = options.ContextMenu ?? true,
            RpcNamespace = options.RpcNamespace,
            RpcMethods = new HashSet<string>(options.RpcMethods ?? Array.Empty<string>(), StringComparer.Ordinal),
            Created = new ManualResetEventSlim(false)
        };

        lock (_sync)
        {
            _windows[id] = record;
        }

        Native.PostThreadMessageW(_uiThreadId, Native.WM_APP_CREATE_WINDOW, (nuint)id, 0);
        record.Created.Wait();

        return id;
    }

    public void LoadUrl(int id, string url)
    {
        if (_windows.TryGetValue(id, out var window))
        {
            window.CurrentUrl = url;
        }
    }

    public void ShowWindow(int id)
    {
        if (TryGetWindowHandle(id, out var hwnd))
        {
            Native.ShowWindow(hwnd, Native.SW_SHOW);
            Native.UpdateWindow(hwnd);
        }
    }

    public void HideWindow(int id)
    {
        if (TryGetWindowHandle(id, out var hwnd))
        {
            Native.ShowWindow(hwnd, Native.SW_HIDE);
        }
    }

    public void CloseWindow(int id)
    {
        if (TryGetWindowHandle(id, out var hwnd))
        {
            Native.PostMessageW(hwnd, Native.WM_CLOSE, 0, 0);
        }
    }

    public void SetTitle(int id, string title)
    {
        if (TryGetWindowHandle(id, out var hwnd))
        {
            Native.SetWindowTextW(hwnd, title);
        }
    }

    public int[] GetWindowSize(int id)
    {
        if (!TryGetWindowHandle(id, out var hwnd) || !Native.GetWindowRect(hwnd, out var rect))
        {
            return [0, 0];
        }

        return [rect.Right - rect.Left, rect.Bottom - rect.Top];
    }

    public int[] GetWindowPosition(int id)
    {
        if (!TryGetWindowHandle(id, out var hwnd) || !Native.GetWindowRect(hwnd, out var rect))
        {
            return [0, 0];
        }

        return [rect.Left, rect.Top];
    }

    public void FocusWindow(int id)
    {
        if (TryGetWindowHandle(id, out var hwnd))
        {
            Native.ShowWindow(hwnd, Native.SW_SHOW);
            Native.SetForegroundWindow(hwnd);
        }
    }

    public void CenterWindow(int id)
    {
        if (!TryGetWindowHandle(id, out var hwnd) || !Native.GetWindowRect(hwnd, out var rect))
        {
            return;
        }

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        var x = Math.Max(0, (Native.GetSystemMetrics(Native.SM_CXSCREEN) - width) / 2);
        var y = Math.Max(0, (Native.GetSystemMetrics(Native.SM_CYSCREEN) - height) / 2);
        Native.SetWindowPos(hwnd, 0, x, y, 0, 0, Native.SWP_NOSIZE | Native.SWP_NOZORDER | Native.SWP_NOACTIVATE);
    }

    public void MinimizeWindow(int id)
    {
        if (TryGetWindowHandle(id, out var hwnd))
        {
            Native.ShowWindow(hwnd, Native.SW_MINIMIZE);
        }
    }

    public void UnminimizeWindow(int id)
    {
        if (TryGetWindowHandle(id, out var hwnd))
        {
            Native.ShowWindow(hwnd, Native.SW_RESTORE);
        }
    }

    public bool IsWindowMinimized(int id)
    {
        return TryGetWindowHandle(id, out var hwnd) && Native.IsIconic(hwnd);
    }

    public void MaximizeWindow(int id)
    {
        if (TryGetWindowHandle(id, out var hwnd))
        {
            Native.ShowWindow(hwnd, Native.SW_MAXIMIZE);
        }
    }

    public void UnmaximizeWindow(int id)
    {
        if (TryGetWindowHandle(id, out var hwnd))
        {
            Native.ShowWindow(hwnd, Native.SW_RESTORE);
        }
    }

    public bool IsWindowMaximized(int id)
    {
        return TryGetWindowHandle(id, out var hwnd) && Native.IsZoomed(hwnd);
    }

    public void SetWindowRpcManifest(int id, string? rpcNamespace, string[] methods)
    {
        if (_windows.TryGetValue(id, out var window))
        {
            window.RpcNamespace = rpcNamespace;
            window.RpcMethods = new HashSet<string>(methods, StringComparer.Ordinal);
        }
    }

    public void SetWindowRpcInvokeSink(Action<int, string, string, string> sink)
    {
        _windowRpcInvokeSink = sink;
    }

    public void ResolveWindowRpc(int id, string requestId, string resultJson)
    {
    }

    public void RejectWindowRpc(int id, string requestId, string error)
    {
    }

    public int CreateTray(TrayOptions options)
    {
        return _nextTrayId++;
    }

    public void SetTrayToolTip(int id, string tooltip)
    {
    }

    public void SetTrayMenu(int id, TrayMenuItem[] menu)
    {
    }

    public void SetTrayEventSink(Action<int, string, string?> sink)
    {
        _trayEventSink = sink;
    }

    public string[] OpenFileDialog(OpenDialogOptions options)
    {
        var fileBuffer = new StringBuilder(65536);
        var filter = BuildNativeFilter(options.Filters);
        var ofn = new Native.OPENFILENAMEW
        {
            lStructSize = Marshal.SizeOf<Native.OPENFILENAMEW>(),
            lpstrFilter = filter,
            lpstrFile = fileBuffer,
            nMaxFile = fileBuffer.Capacity,
            lpstrTitle = options.Title ?? "Open File",
            Flags = Native.OFN_EXPLORER | Native.OFN_PATHMUSTEXIST | Native.OFN_FILEMUSTEXIST
        };

        if (options.AllowMultiple == true)
        {
            ofn.Flags |= Native.OFN_ALLOWMULTISELECT;
        }

        if (!Native.GetOpenFileNameW(ref ofn))
        {
            return Array.Empty<string>();
        }

        return ParseOpenFileBuffer(fileBuffer.ToString());
    }

    public string? SaveFileDialog(SaveDialogOptions options)
    {
        var fileBuffer = new StringBuilder(Math.Max(4096, (options.DefaultPath?.Length ?? 0) + 16));
        if (!string.IsNullOrWhiteSpace(options.DefaultPath))
        {
            fileBuffer.Append(options.DefaultPath);
        }

        var ofn = new Native.OPENFILENAMEW
        {
            lStructSize = Marshal.SizeOf<Native.OPENFILENAMEW>(),
            lpstrFilter = BuildNativeFilter(options.Filters),
            lpstrFile = fileBuffer,
            nMaxFile = fileBuffer.Capacity,
            lpstrTitle = options.Title ?? "Save File",
            Flags = Native.OFN_EXPLORER | Native.OFN_PATHMUSTEXIST | Native.OFN_OVERWRITEPROMPT
        };

        if (!Native.GetSaveFileNameW(ref ofn))
        {
            return null;
        }

        var value = fileBuffer.ToString();
        var firstNull = value.IndexOf('\0');
        if (firstNull >= 0)
        {
            value = value[..firstNull];
        }

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public MessageBoxResult ShowMessageBox(MessageBoxOptions options)
    {
        var count = options.Buttons?.Length ?? 0;
        var style = count switch
        {
            <= 1 => Native.MB_OK,
            2 => Native.MB_OKCANCEL,
            _ => Native.MB_YESNOCANCEL
        };

        var text = string.IsNullOrWhiteSpace(options.Detail)
            ? options.Message
            : $"{options.Message}\n\n{options.Detail}";

        var result = Native.MessageBoxW(0, text, options.Title ?? "Message", style | Native.MB_ICONINFORMATION);
        var response = result switch
        {
            Native.IDOK => 0,
            Native.IDCANCEL => Math.Min(1, Math.Max(0, count - 1)),
            Native.IDYES => 0,
            Native.IDNO => 1,
            _ => 0
        };

        return new MessageBoxResult { Response = response };
    }

    public void OpenExternal(string url)
    {
        StartShell(url);
    }

    public void OpenPath(string path)
    {
        StartShell(path);
    }

    public void RevealPath(string path)
    {
        if (File.Exists(path))
        {
            StartProcess("explorer.exe", $"/select,\"{path}\"");
            return;
        }

        StartShell(path);
    }

    public void TrashPath(string path)
    {
        throw new PlatformNotSupportedException("Trash operation requires a platform-specific host implementation.");
    }

    public string JoinPath(string[] parts)
    {
        return parts.Length == 0 ? string.Empty : Path.Combine(parts);
    }

    public string ResolvePath(string[] parts)
    {
        return parts.Length == 0 ? Path.GetFullPath(Environment.CurrentDirectory) : Path.GetFullPath(Path.Combine(parts));
    }

    public string GetUserDataPath()
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    }

    public string GetLocalDataPath()
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    }

    public string GetTempPath()
    {
        return Path.GetTempPath();
    }

    public string GetCurrentWorkingDirectory()
    {
        return Environment.CurrentDirectory;
    }

    public string GetBaseExecutableDirectory()
    {
        return AppContext.BaseDirectory;
    }

    public string ReadTextFile(string path)
    {
        return File.ReadAllText(path);
    }

    public void WriteTextFile(string path, string content)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(path, content);
    }

    public void AppendTextFile(string path, string content)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.AppendAllText(path, content);
    }

    public bool PathExists(string path)
    {
        return File.Exists(path) || Directory.Exists(path);
    }

    public void CreateDirectory(string path, bool recursive)
    {
        if (recursive)
        {
            Directory.CreateDirectory(path);
            return;
        }

        var parent = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(parent) && !Directory.Exists(parent))
        {
            throw new DirectoryNotFoundException($"Parent directory does not exist: {parent}");
        }

        Directory.CreateDirectory(path);
    }

    public string[] ReadDirectory(string path)
    {
        return Directory.GetFileSystemEntries(path);
    }

    public void RemovePath(string path, bool recursive)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
            return;
        }

        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive);
        }
    }

    private void EnsureUiStarted()
    {
        lock (_sync)
        {
            if (_running && _uiThreadId != 0)
            {
                return;
            }

            _quitRequested = false;
            _uiThread = new Thread(UiThreadMain)
            {
                IsBackground = false,
                Name = "arctron-ui"
            };
            _uiThread.SetApartmentState(ApartmentState.STA);
            _uiThread.Start();
            _running = true;
        }

        _uiThreadReady.WaitOne();
    }

    private void UiThreadMain()
    {
        s_current = this;
        _uiThreadId = Native.GetCurrentThreadId();

        RegisterWindowClass();
        _uiThreadReady.Set();

        while (!_quitRequested && Native.GetMessageW(out var msg, 0, 0, 0) > 0)
        {
            if (msg.message == Native.WM_APP_CREATE_WINDOW)
            {
                CreateNativeWindow((int)msg.wParam);
                continue;
            }

            Native.TranslateMessage(ref msg);
            Native.DispatchMessageW(ref msg);
        }

        CleanupWindows();

        if (_windowClassAtom != 0)
        {
            Native.UnregisterClassW(WindowClassName, Native.GetModuleHandleW(null));
            _windowClassAtom = 0;
        }

        s_current = null;
    }

    private void RegisterWindowClass()
    {
        var wndClass = new Native.WNDCLASSW
        {
            hInstance = Native.GetModuleHandleW(null),
            lpszClassName = WindowClassName,
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(WndProcDelegateInstance),
            hCursor = Native.LoadCursorW(0, (nint)Native.IDC_ARROW)
        };

        _windowClassAtom = Native.RegisterClassW(ref wndClass);
        if (_windowClassAtom == 0)
        {
            throw new InvalidOperationException("Failed to register window class.");
        }
    }

    private void CreateNativeWindow(int id)
    {
        WindowRecord? record;
        lock (_sync)
        {
            _windows.TryGetValue(id, out record);
        }

        if (record == null)
        {
            return;
        }

        var style = record.Frameless
            ? Native.WS_POPUP | Native.WS_THICKFRAME
            : Native.WS_OVERLAPPEDWINDOW;

        var x = record.X ?? Native.CW_USEDEFAULT;
        var y = record.Y ?? Native.CW_USEDEFAULT;

        var hwnd = Native.CreateWindowExW(
            0,
            WindowClassName,
            record.Title,
            style,
            x,
            y,
            record.Width,
            record.Height,
            0,
            0,
            Native.GetModuleHandleW(null),
            0);

        lock (_sync)
        {
            record.Hwnd = hwnd;
            if (hwnd != 0)
            {
                _hwndToId[hwnd] = id;
            }
        }

        if (hwnd != 0 && record.Visible)
        {
            Native.ShowWindow(hwnd, Native.SW_SHOW);
            Native.UpdateWindow(hwnd);
        }

        record.Created.Set();
    }

    private void CleanupWindows()
    {
        List<nint> handles;
        lock (_sync)
        {
            handles = _windows.Values.Where(w => w.Hwnd != 0).Select(w => w.Hwnd).ToList();
        }

        foreach (var hwnd in handles)
        {
            Native.DestroyWindow(hwnd);
        }

        lock (_sync)
        {
            _hwndToId.Clear();
            foreach (var window in _windows.Values)
            {
                window.Created.Dispose();
            }
            _windows.Clear();
        }
    }

    private bool TryGetWindowHandle(int id, out nint hwnd)
    {
        lock (_sync)
        {
            if (_windows.TryGetValue(id, out var record) && record.Hwnd != 0)
            {
                hwnd = record.Hwnd;
                return true;
            }
        }

        hwnd = 0;
        return false;
    }

    private static nint WndProc(nint hwnd, uint msg, nuint wParam, nint lParam)
    {
        var current = s_current;

        if (msg == Native.WM_CLOSE)
        {
            Native.DestroyWindow(hwnd);
            return 0;
        }

        if (msg == Native.WM_DESTROY && current != null)
        {
            var shouldQuit = false;

            lock (current._sync)
            {
                if (current._hwndToId.TryGetValue(hwnd, out var id))
                {
                    current._hwndToId.Remove(hwnd);
                    if (current._windows.TryGetValue(id, out var record))
                    {
                        record.Hwnd = 0;
                        record.Created.Dispose();
                        current._windows.Remove(id);
                    }
                }

                if (current._windows.Count == 0)
                {
                    shouldQuit = true;
                }
            }

            if (shouldQuit)
            {
                Native.PostQuitMessage(0);
            }

            return 0;
        }

        return Native.DefWindowProcW(hwnd, msg, wParam, lParam);
    }

    private static string BuildNativeFilter(DialogFilter[]? filters)
    {
        if (filters == null || filters.Length == 0)
        {
            return "All files (*.*)\0*.*\0\0";
        }

        var parts = new List<string>();
        foreach (var filter in filters)
        {
            var extensions = (filter.Extensions ?? Array.Empty<string>())
                .Select(ext => ext.Trim().TrimStart('.'))
                .Where(ext => !string.IsNullOrWhiteSpace(ext))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (extensions.Length == 0)
            {
                continue;
            }

            var pattern = string.Join(';', extensions.Select(ext => $"*.{ext}"));
            var label = string.IsNullOrWhiteSpace(filter.Name) ? pattern : filter.Name;
            parts.Add(label);
            parts.Add(pattern);
        }

        if (parts.Count == 0)
        {
            return "All files (*.*)\0*.*\0\0";
        }

        return string.Join("\0", parts) + "\0\0";
    }

    private static string[] ParseOpenFileBuffer(string raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return Array.Empty<string>();
        }

        var tokens = new List<string>();
        var start = 0;
        for (var i = 0; i < raw.Length; i++)
        {
            if (raw[i] != '\0')
            {
                continue;
            }

            if (i == start)
            {
                break;
            }

            tokens.Add(raw[start..i]);
            start = i + 1;
        }

        if (tokens.Count == 0)
        {
            return Array.Empty<string>();
        }

        if (tokens.Count == 1)
        {
            return [tokens[0]];
        }

        var directory = tokens[0];
        return tokens.Skip(1).Select(file => Path.Combine(directory, file)).ToArray();
    }

    private static void StartShell(string target)
    {
        StartProcess(target, null, useShellExecute: true);
    }

    private static void StartProcess(string fileName, string? arguments, bool useShellExecute = false)
    {
        var info = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = useShellExecute
        };

        if (!string.IsNullOrWhiteSpace(arguments))
        {
            info.Arguments = arguments;
        }

        Process.Start(info);
    }

    private const string WindowClassName = "ArctronHostWindowClass";

    private sealed class WindowRecord
    {
        public int Id { get; init; }
        public nint Hwnd { get; set; }
        public int Width { get; init; }
        public int Height { get; init; }
        public int? X { get; init; }
        public int? Y { get; init; }
        public string Title { get; init; } = "Arctron";
        public bool Visible { get; init; }
        public bool Frameless { get; init; }
        public string? CurrentUrl { get; set; }
        public bool DevTools { get; init; }
        public bool ContextMenu { get; init; }
        public string? RpcNamespace { get; set; }
        public HashSet<string> RpcMethods { get; set; } = new(StringComparer.Ordinal);
        public required ManualResetEventSlim Created { get; init; }
    }

    private static class Native
    {
        public const int CW_USEDEFAULT = unchecked((int)0x80000000);

        public const uint WM_CLOSE = 0x0010;
        public const uint WM_DESTROY = 0x0002;
        public const uint WM_QUIT = 0x0012;
        public const uint WM_USER = 0x0400;
        public const uint WM_APP_CREATE_WINDOW = WM_USER + 101;

        public const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
        public const uint WS_POPUP = 0x80000000;
        public const uint WS_THICKFRAME = 0x00040000;

        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;
        public const int SW_MINIMIZE = 6;
        public const int SW_MAXIMIZE = 3;
        public const int SW_RESTORE = 9;

        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOACTIVATE = 0x0010;

        public const int SM_CXSCREEN = 0;
        public const int SM_CYSCREEN = 1;

        public const int OFN_EXPLORER = 0x00080000;
        public const int OFN_PATHMUSTEXIST = 0x00000800;
        public const int OFN_FILEMUSTEXIST = 0x00001000;
        public const int OFN_ALLOWMULTISELECT = 0x00000200;
        public const int OFN_OVERWRITEPROMPT = 0x00000002;

        public const uint MB_OK = 0x00000000;
        public const uint MB_OKCANCEL = 0x00000001;
        public const uint MB_YESNOCANCEL = 0x00000003;
        public const uint MB_ICONINFORMATION = 0x00000040;

        public const int IDOK = 1;
        public const int IDCANCEL = 2;
        public const int IDYES = 6;
        public const int IDNO = 7;

        public const int IDC_ARROW = 32512;

        public delegate nint WndProcDelegate(nint hwnd, uint msg, nuint wParam, nint lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSG
        {
            public nint hwnd;
            public uint message;
            public nuint wParam;
            public nint lParam;
            public uint time;
            public POINT pt;
            public uint lPrivate;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WNDCLASSW
        {
            public uint style;
            public nint lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public nint hInstance;
            public nint hIcon;
            public nint hCursor;
            public nint hbrBackground;
            public string? lpszMenuName;
            public string lpszClassName;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct OPENFILENAMEW
        {
            public int lStructSize;
            public nint hwndOwner;
            public nint hInstance;
            public string lpstrFilter;
            public string? lpstrCustomFilter;
            public int nMaxCustFilter;
            public int nFilterIndex;
            public StringBuilder lpstrFile;
            public int nMaxFile;
            public string? lpstrFileTitle;
            public int nMaxFileTitle;
            public string? lpstrInitialDir;
            public string? lpstrTitle;
            public int Flags;
            public short nFileOffset;
            public short nFileExtension;
            public string? lpstrDefExt;
            public nint lCustData;
            public nint lpfnHook;
            public string? lpTemplateName;
            public nint pvReserved;
            public int dwReserved;
            public int FlagsEx;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern nint CreateWindowExW(uint exStyle, string className, string windowName, uint style, int x, int y, int width, int height, nint parent, nint menu, nint instance, nint param);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyWindow(nint hwnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(nint hwnd, int command);

        [DllImport("user32.dll")]
        public static extern bool UpdateWindow(nint hwnd);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(nint hwnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool SetWindowTextW(nint hwnd, string text);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(nint hwnd, out RECT rect);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(nint hwnd, nint insertAfter, int x, int y, int width, int height, uint flags);

        [DllImport("user32.dll")]
        public static extern bool IsIconic(nint hwnd);

        [DllImport("user32.dll")]
        public static extern bool IsZoomed(nint hwnd);

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int index);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        public static extern bool PostThreadMessageW(uint threadId, uint msg, nuint wParam, nint lParam);

        [DllImport("user32.dll")]
        public static extern bool PostMessageW(nint hwnd, uint msg, nuint wParam, nint lParam);

        [DllImport("user32.dll")]
        public static extern int GetMessageW(out MSG msg, nint hwnd, uint minFilter, uint maxFilter);

        [DllImport("user32.dll")]
        public static extern bool TranslateMessage(ref MSG msg);

        [DllImport("user32.dll")]
        public static extern nint DispatchMessageW(ref MSG msg);

        [DllImport("user32.dll")]
        public static extern void PostQuitMessage(int exitCode);

        [DllImport("user32.dll")]
        public static extern nint DefWindowProcW(nint hwnd, uint msg, nuint wParam, nint lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern ushort RegisterClassW(ref WNDCLASSW wndClass);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool UnregisterClassW(string className, nint instance);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern nint GetModuleHandleW(string? moduleName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern nint LoadCursorW(nint instance, nint cursor);

        [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool GetOpenFileNameW(ref OPENFILENAMEW openFileName);

        [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool GetSaveFileNameW(ref OPENFILENAMEW openFileName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int MessageBoxW(nint hwnd, string text, string caption, uint type);
    }
}
