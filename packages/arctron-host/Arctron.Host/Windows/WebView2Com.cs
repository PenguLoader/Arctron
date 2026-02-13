using Diga.WebView2.Interop;
using System.Runtime.InteropServices.Marshalling;

namespace Arctron.Host.Windows;

[GeneratedComClass]
partial class WebView2EnvironmentOptions : ICoreWebView2EnvironmentOptions
{
    private string? _additionalArgs;
    private string? _language;
    private string? _targetVersion = "90.0.0.0";
    private int _allowSso;

    public WebView2EnvironmentOptions(string? additionalArgs)
    {
        _additionalArgs = additionalArgs;
    }

    public string GetAdditionalBrowserArguments()
    {
        return _additionalArgs ?? string.Empty;
    }

    public void SetAdditionalBrowserArguments(string value)
    {
        _additionalArgs = value;
    }

    public string GetLanguage() => _language ?? string.Empty;

    public void SetLanguage(string value)
    {
        _language = value;
    }

    public string GetTargetCompatibleBrowserVersion() => _targetVersion ?? string.Empty;

    public void SetTargetCompatibleBrowserVersion(string value)
    {
        _targetVersion = value;
    }

    public int GetAllowSingleSignOnUsingOSPrimaryAccount() => _allowSso;

    public void SetAllowSingleSignOnUsingOSPrimaryAccount(int value)
    {
        _allowSso = value;
    }
}

[GeneratedComClass]
internal partial class AddScriptCompletedHandler : ICoreWebView2AddScriptToExecuteOnDocumentCreatedCompletedHandler
{
    public void Invoke(int errorCode, string id)
    {
    }
}

[GeneratedComClass]
internal partial class EnvironmentCompletedHandler : ICoreWebView2CreateCoreWebView2EnvironmentCompletedHandler
{
    private readonly WebView2Host _host;

    public EnvironmentCompletedHandler(WebView2Host host)
    {
        _host = host;
    }

    public void Invoke(int result, ICoreWebView2Environment createdEnvironment)
        => _host.OnEnvironmentCompleted(result, createdEnvironment);
}

[GeneratedComClass]
internal partial class ControllerCompletedHandler : ICoreWebView2CreateCoreWebView2ControllerCompletedHandler
{
    private readonly WebView2Host _host;

    public ControllerCompletedHandler(WebView2Host host)
    {
        _host = host;
    }

    public void Invoke(int result, ICoreWebView2Controller createdController)
        => _host.OnControllerCompleted(result, createdController);
}

[GeneratedComClass]
internal partial class WebMessageReceivedHandler : ICoreWebView2WebMessageReceivedEventHandler
{
    private readonly WebView2Host _host;

    public WebMessageReceivedHandler(WebView2Host host)
    {
        _host = host;
    }

    public void Invoke(ICoreWebView2 sender, ICoreWebView2WebMessageReceivedEventArgs args)
        => _host.OnWebMessageReceived(sender, args);
}

[GeneratedComClass]
internal partial class NavigationCompletedHandler : ICoreWebView2NavigationCompletedEventHandler
{
    private readonly WebView2Host _host;

    public NavigationCompletedHandler(WebView2Host host)
    {
        _host = host;
    }

    public void Invoke(ICoreWebView2 sender, ICoreWebView2NavigationCompletedEventArgs args)
        => _host.OnNavigationCompleted(sender, args);
}