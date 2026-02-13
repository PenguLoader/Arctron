using static Vanara.PInvoke.User32;

namespace Arctron.Host.Windows;

//sealed class WindowsApp : WebViewApp
//{
//    private Win32Window _window;
//    private WebView2Host _webView;

//    public WindowsApp()
//    {
//        if (!WebView2Host.IsRuntimeAvailable())
//        {
//            MessageBox(0,
//                "Microsoft Edge WebView2 Runtime is not installed. Please install it to run this app.",
//                "Pengu Loader",
//                MB_FLAGS.MB_OK | MB_FLAGS.MB_ICONWARNING);

//            Environment.Exit(-1);
//        }

//        _window = new Win32Window();
//        _webView = new WebView2Host();
//    }

//    protected override void CreateWindow()
//    {
//        _window.Init(Width, Height, Caption);
//    }

//    protected override void CreateWebView()
//    {
//        _window.Resized += _webView.Resize;
//        _webView.Ready += _window.Show;

//        _ = _webView.InitAsync(Url, _window.Handle, Debug);
//    }

//    protected override void ShowWindow()
//    {
//        _window.Show();
//    }

//    protected override int RunApp()
//    {
//        return _window.RunMessageLoop();
//    }
//}