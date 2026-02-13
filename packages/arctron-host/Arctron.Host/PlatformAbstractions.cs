using System.Diagnostics;
using System.Threading;

namespace Arctron.Host;

public interface IHostPlatform
{
    int Run();
    void Quit();

    int CreateWindow(WindowOptions options);
    void LoadUrl(int id, string url);
    void ShowWindow(int id);
    void HideWindow(int id);
    void CloseWindow(int id);
    void SetTitle(int id, string title);
    int[] GetWindowSize(int id);
    int[] GetWindowPosition(int id);
    void FocusWindow(int id);
    void CenterWindow(int id);
    void MinimizeWindow(int id);
    void UnminimizeWindow(int id);
    bool IsWindowMinimized(int id);
    void MaximizeWindow(int id);
    void UnmaximizeWindow(int id);
    bool IsWindowMaximized(int id);
    void SetWindowRpcManifest(int id, string? rpcNamespace, string[] methods);
    void SetWindowRpcInvokeSink(Action<int, string, string, string> sink);
    void ResolveWindowRpc(int id, string requestId, string resultJson);
    void RejectWindowRpc(int id, string requestId, string error);

    int CreateTray(TrayOptions options);
    void SetTrayToolTip(int id, string tooltip);
    void SetTrayMenu(int id, TrayMenuItem[] menu);
    void SetTrayEventSink(Action<int, string, string?> sink);

    string[] OpenFileDialog(OpenDialogOptions options);
    string? SaveFileDialog(SaveDialogOptions options);
    MessageBoxResult ShowMessageBox(MessageBoxOptions options);

    void OpenExternal(string url);
    void OpenPath(string path);
    void RevealPath(string path);
    void TrashPath(string path);

    string JoinPath(string[] parts);
    string ResolvePath(string[] parts);
    string GetUserDataPath();
    string GetLocalDataPath();
    string GetTempPath();
    string GetCurrentWorkingDirectory();
    string GetBaseExecutableDirectory();

    string ReadTextFile(string path);
    void WriteTextFile(string path, string content);
    void AppendTextFile(string path, string content);
    bool PathExists(string path);
    void CreateDirectory(string path, bool recursive);
    string[] ReadDirectory(string path);
    void RemovePath(string path, bool recursive);
}

public static class HostPlatformFactory
{
    public static IHostPlatform Create()
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsHostPlatform();
        }

        return new DefaultHostPlatform();
    }
}

public sealed class DefaultHostPlatform : IHostPlatform
{
    private readonly ManualResetEventSlim _runLatch = new(false);
    private int _nextWindowId = 1;
    private int _nextTrayId = 1;
    private readonly Dictionary<int, WindowState> _windows = new();
    private Action<int, string, string?>? _trayEventSink;
    private Action<int, string, string, string>? _windowRpcInvokeSink;

    public int Run()
    {
        _runLatch.Wait();
        return 0;
    }

    public void Quit()
    {
        _runLatch.Set();
    }

    public int CreateWindow(WindowOptions options)
    {
        var id = _nextWindowId++;
        var width = options.Width ?? 1024;
        var height = options.Height ?? 768;

        _windows[id] = new WindowState
        {
            Width = width,
            Height = height,
            X = options.X ?? 0,
            Y = options.Y ?? 0,
            Title = options.Title ?? "Arctron",
            Visible = options.Show ?? true,
            Frameless = options.Frameless ?? false,
            CurrentUrl = options.InitialUrl,
            DevTools = options.DevTools ?? false,
            ContextMenu = options.ContextMenu ?? true
        };

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
        if (_windows.TryGetValue(id, out var window))
        {
            window.Visible = true;
        }
    }

    public void HideWindow(int id)
    {
        if (_windows.TryGetValue(id, out var window))
        {
            window.Visible = false;
        }
    }

    public void CloseWindow(int id)
    {
        _windows.Remove(id);
    }

    public void SetTitle(int id, string title)
    {
        if (_windows.TryGetValue(id, out var window))
        {
            window.Title = title;
        }
    }

    public int[] GetWindowSize(int id)
    {
        if (_windows.TryGetValue(id, out var window))
        {
            return new[] { window.Width, window.Height };
        }

        return new[] { 0, 0 };
    }

    public int[] GetWindowPosition(int id)
    {
        if (_windows.TryGetValue(id, out var window))
        {
            return new[] { window.X, window.Y };
        }

        return new[] { 0, 0 };
    }

    public void FocusWindow(int id)
    {
        if (_windows.TryGetValue(id, out var window))
        {
            window.Visible = true;
        }
    }

    public void CenterWindow(int id)
    {
        if (!_windows.TryGetValue(id, out var window))
        {
            return;
        }

        const int screenWidth = 1920;
        const int screenHeight = 1080;
        window.X = Math.Max(0, (screenWidth - window.Width) / 2);
        window.Y = Math.Max(0, (screenHeight - window.Height) / 2);
    }

    public void MinimizeWindow(int id)
    {
        if (_windows.TryGetValue(id, out var window))
        {
            window.Minimized = true;
            window.Maximized = false;
        }
    }

    public void UnminimizeWindow(int id)
    {
        if (_windows.TryGetValue(id, out var window))
        {
            window.Minimized = false;
        }
    }

    public bool IsWindowMinimized(int id)
    {
        return _windows.TryGetValue(id, out var window) && window.Minimized;
    }

    public void MaximizeWindow(int id)
    {
        if (_windows.TryGetValue(id, out var window))
        {
            window.Maximized = true;
            window.Minimized = false;
        }
    }

    public void UnmaximizeWindow(int id)
    {
        if (_windows.TryGetValue(id, out var window))
        {
            window.Maximized = false;
        }
    }

    public bool IsWindowMaximized(int id)
    {
        return _windows.TryGetValue(id, out var window) && window.Maximized;
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
        return Array.Empty<string>();
    }

    public string? SaveFileDialog(SaveDialogOptions options)
    {
        return null;
    }

    public MessageBoxResult ShowMessageBox(MessageBoxOptions options)
    {
        Console.WriteLine($"[{options.Title ?? "Message"}] {options.Message}");
        return new MessageBoxResult { Response = 0 };
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
        if (OperatingSystem.IsWindows())
        {
            if (File.Exists(path))
            {
                StartProcess("explorer.exe", $"/select,\"{path}\"");
                return;
            }

            StartShell(path);
            return;
        }

        if (OperatingSystem.IsMacOS())
        {
            if (File.Exists(path))
            {
                StartProcess("open", $"-R \"{path}\"");
                return;
            }

            StartProcess("open", $"\"{path}\"");
            return;
        }

        var target = File.Exists(path) ? Path.GetDirectoryName(path) ?? path : path;
        StartProcess("xdg-open", $"\"{target}\"");
    }

    public void TrashPath(string path)
    {
        throw new PlatformNotSupportedException("Trash operation requires a platform-specific host implementation.");
    }

    public string JoinPath(string[] parts)
    {
        if (parts.Length == 0)
        {
            return string.Empty;
        }

        return Path.Combine(parts);
    }

    public string ResolvePath(string[] parts)
    {
        if (parts.Length == 0)
        {
            return Path.GetFullPath(Environment.CurrentDirectory);
        }

        var combined = Path.Combine(parts);
        return Path.GetFullPath(combined);
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

    private static void StartShell(string target)
    {
        StartProcess(target, null, useShellExecute: true);
    }

    private static void StartProcess(string fileName, string? arguments, bool useShellExecute = false)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = useShellExecute
        };

        if (!string.IsNullOrWhiteSpace(arguments))
        {
            startInfo.Arguments = arguments;
        }

        Process.Start(startInfo);
    }

    private sealed class WindowState
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public string Title { get; set; } = "Arctron";
        public bool Visible { get; set; }
        public bool Frameless { get; set; }
        public string? CurrentUrl { get; set; }
        public bool DevTools { get; set; }
        public bool ContextMenu { get; set; }
        public bool Minimized { get; set; }
        public bool Maximized { get; set; }
        public string? RpcNamespace { get; set; }
        public HashSet<string> RpcMethods { get; set; } = new(StringComparer.Ordinal);
    }
}
