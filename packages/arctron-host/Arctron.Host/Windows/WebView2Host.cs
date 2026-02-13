using Diga.WebView2.Interop;
using System.Runtime.InteropServices;
using static Vanara.PInvoke.User32;

namespace Arctron.Host.Windows;

sealed class WebView2Host
{
    private string? _url;
    private nint _hwnd;
    private bool _debug;

    private ICoreWebView2Controller? _controller;
    private ICoreWebView2? _webView;
    private ICoreWebView2WebMessageReceivedEventHandler? _webMessageHandler;
    private ICoreWebView2NavigationCompletedEventHandler? _navCompletedHandler;
    private WebView2EnvironmentOptions? _envOptions;
    private ICoreWebView2CreateCoreWebView2EnvironmentCompletedHandler? _envHandler;
    private ICoreWebView2CreateCoreWebView2ControllerCompletedHandler? _controllerHandler;
    private ICoreWebView2AddScriptToExecuteOnDocumentCreatedCompletedHandler? _addScriptHandler;
    private TaskCompletionSource<bool>? _initTcs;

    public event Action? Ready;

    public async Task InitAsync(string url, nint hwnd, bool debug)
    {
        _url = url;
        _hwnd = hwnd;
        _debug = debug;

        var tcs = new TaskCompletionSource<bool>();
        _initTcs = tcs;

        try
        {
            _envOptions = new WebView2EnvironmentOptions("--enable-features=msWebView2EnableDraggableRegions");
            _envHandler = new EnvironmentCompletedHandler(this);
            var hr = WebView2Loader.CreateCoreWebView2EnvironmentWithOptions(null, null, _envOptions, _envHandler);

            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WebView2 initialization failed: {ex}");
            tcs.TrySetException(ex);
        }

        await tcs.Task.ConfigureAwait(false);
    }

    public static bool IsRuntimeAvailable()
    {
        var hr = WebView2Loader.GetAvailableCoreWebView2BrowserVersionString(null, out var versionPtr);
        if (hr < 0)
        {
            return false;
        }
        if (versionPtr != 0)
        {
            WebView2Loader.CoTaskMemFree(versionPtr);
        }
        return true;
    }

    public void Resize(int width, int height)
    {
        if (_controller is null)
        {
            return;
        }

        var bounds = new tagRECT
        {
            left = 0,
            top = 0,
            right = Math.Max(width, 1),
            bottom = Math.Max(height, 1)
        };

        _controller.SetBounds(bounds);
    }

    private void ConfigureSettings(ICoreWebView2 webView)
    {
        var settings = webView.GetSettings();
        settings.SetAreDevToolsEnabled(_debug ? 1 : 0);
        settings.SetAreDefaultContextMenusEnabled(_debug ? 1 : 0);
        settings.SetAreHostObjectsAllowed(0);
    }

    private void OnWebMessageReceived(ICoreWebView2WebMessageReceivedEventArgs e)
    {
        var message = e.TryGetWebMessageAsString();
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (message == "ping")
        {
            _webView?.PostWebMessageAsString("pong");
            return;
        }

        _webView?.PostWebMessageAsString($"echo:{message}");
    }

    internal void OnEnvironmentCompleted(int result, ICoreWebView2Environment createdEnvironment)
    {
        if (result < 0)
        {
            _initTcs?.TrySetException(new COMException("WebView2 environment creation failed.", result));
            return;
        }

        _controllerHandler = new ControllerCompletedHandler(this);
        try
        {
            createdEnvironment.CreateCoreWebView2Controller(_hwnd, _controllerHandler);
        }
        catch (COMException ex)
        {
            _initTcs?.TrySetException(ex);
        }
    }

    internal void OnControllerCompleted(int result, ICoreWebView2Controller createdController)
    {
        if (result < 0)
        {
            _initTcs?.TrySetException(new COMException("WebView2 controller creation failed.", result));
            return;
        }

        _controller = createdController;
        createdController.SetIsVisible(1);

        var webView = createdController.GetCoreWebView2();
        _webView = webView;

        ConfigureSettings(webView);

        _webMessageHandler = new WebMessageReceivedHandler(this);
        webView.add_WebMessageReceived(_webMessageHandler, out _);

        _navCompletedHandler = new NavigationCompletedHandler(this);
        webView.add_NavigationCompleted(_navCompletedHandler, out _);

        var script = "window.onload = (function(){" +
                     "window.pengu={send:(msg)=>window.chrome.webview.postMessage(msg)};" +
                     "const style=document.createElement('style');" +
                     "style.textContent='" +
                     "#pengu-titlebar{position:fixed;top:0;left:0;right:0;height:32px;background:#111;color:#fff;display:flex;align-items:center;padding:0 12px;z-index:999999;-webkit-app-region:drag;font:12px/32px Segoe UI, sans-serif;}' +" +
                     "'body{padding-top:32px !important;}' +" +
                     "'.pengu-nodrag{-webkit-app-region:no-drag;}';" +
                     "document.documentElement.appendChild(style);" +
                     "const bar=document.createElement('div');" +
                     "bar.id='pengu-titlebar';" +
                     "bar.textContent='Pengu.Loader';" +
                     "document.addEventListener('DOMContentLoaded',()=>{document.body.appendChild(bar);},{once:true});" +
                     "});";

        _addScriptHandler = new AddScriptCompletedHandler();
        webView.AddScriptToExecuteOnDocumentCreated(script, _addScriptHandler);

        GetClientRect(_hwnd, out var rect);
        Resize(rect.Width, rect.Height);

        webView.Navigate(_url);

        Ready?.Invoke();
        _initTcs?.TrySetResult(true);
    }

    internal void OnWebMessageReceived(ICoreWebView2 sender, ICoreWebView2WebMessageReceivedEventArgs args)
    {
        OnWebMessageReceived(args);
    }

    internal void OnNavigationCompleted(ICoreWebView2 sender, ICoreWebView2NavigationCompletedEventArgs args)
    {
        _webView?.PostWebMessageAsString("host:ready");
    }
}