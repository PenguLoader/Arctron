using System.Runtime.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.User32;
using static Vanara.PInvoke.DwmApi;

namespace Arctron.Host.Windows;

sealed class Win32Window
{
    private HWND _hwnd;
    private WindowProc? _wndproc;

    public nint Handle => (nint)_hwnd;

    public event Action<int, int>? Resized;

    public void Init(int width, int height, string caption)
    {
        var instance = GetModuleHandle(null);
        var className = nameof(Win32Window);
        var appIcon = LoadIcon(instance, new SafeResourceId(IDI_APPLICATION));

        _wndproc = WndProc;

        var wc = new WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
            style = WindowClassStyles.CS_HREDRAW | WindowClassStyles.CS_VREDRAW,
            lpfnWndProc = _wndproc,
            hInstance = instance,
            hCursor = LoadCursor(0, new SafeResourceId(IDC_ARROW)),
            hbrBackground = GetSysColorBrush(SystemColorIndex.COLOR_WINDOW),
            hIcon = appIcon,
            hIconSm = appIcon,
            lpszClassName = className
        };

        RegisterClassEx(in wc);

        int x = (GetSystemMetrics(SystemMetric.SM_CXSCREEN) - width) / 2;
        int y = (GetSystemMetrics(SystemMetric.SM_CYSCREEN) - height) / 2;

        _hwnd = CreateWindowEx(
            WindowStylesEx.WS_EX_APPWINDOW,
            className,
            caption,
            WindowStyles.WS_POPUP,
            x,
            y,
            width,
            height,
            0,
            0,
            instance,
            0);

        EnableShadow(_hwnd);
    }

    public void Show()
    {
        ShowWindow(_hwnd, ShowWindowCommand.SW_SHOW);
        UpdateWindow(_hwnd);
    }

    public int RunMessageLoop()
    {
        MSG msg;

        while (GetMessage(out msg, 0, 0, 0) != 0)
        {
            TranslateMessage(msg);
            DispatchMessage(msg);
        }

        return (int)msg.wParam;
    }

    private static void EnableShadow(HWND hwnd)
    {
        unsafe
        {
            var value = DWMNCRENDERINGPOLICY.DWMNCRP_ENABLED;
            var pval = &value;

            DwmSetWindowAttribute(hwnd,
                DWMWINDOWATTRIBUTE.DWMWA_NCRENDERING_POLICY,
                (nint)pval, sizeof(int));
        }

        var margins = new MARGINS(0, 0, 1, 0);
        DwmExtendFrameIntoClientArea(hwnd, in margins);
    }

    private nint WndProc(HWND hwnd, uint msg, nint wParam, nint lParam)
    {
        switch ((WindowMessage)msg)
        {
            case WindowMessage.WM_SIZE:
                int width = Macros.LOWORD(lParam);
                int height = Macros.HIWORD(lParam);
                Resized?.Invoke(width, height);
                break;

            case WindowMessage.WM_DESTROY:
                PostQuitMessage(0);
                break;
        }

        return DefWindowProc(hwnd, msg, wParam, lParam);
    }
}