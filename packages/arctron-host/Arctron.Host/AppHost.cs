namespace Arctron.Host;

public sealed class AppHost
{
    private readonly IHostPlatform _platform;
    private readonly NativeBridge _bridge;
    private readonly JsRuntime _runtime;

    public AppHost(string code)
    {
        _platform = HostPlatformFactory.Create();
        _bridge = new NativeBridge(_platform);
        _runtime = new JsRuntime(_bridge);
        _runtime.RunScript(code);
    }

    public int Run()
    {
        return _platform.Run();
    }
}