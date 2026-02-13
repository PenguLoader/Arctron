using Diga.WebView2.Interop;
using System.Runtime.InteropServices;

namespace Arctron.Host.Windows;

static partial class WebView2Loader
{
    [LibraryImport("WebView2Loader.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int CreateCoreWebView2EnvironmentWithOptions(
        string? browserExeFolder,
        string? userDataFolder,
        [MarshalAs(UnmanagedType.Interface)] ICoreWebView2EnvironmentOptions? environmentOptions,
        [MarshalAs(UnmanagedType.Interface)] ICoreWebView2CreateCoreWebView2EnvironmentCompletedHandler environmentCreatedHandler);

    [LibraryImport("WebView2Loader.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int GetAvailableCoreWebView2BrowserVersionString(
        string? browserExeFolder,
        out nint versionInfo);

    [LibraryImport("ole32.dll")]
    public static partial void CoTaskMemFree(nint ptr);
}