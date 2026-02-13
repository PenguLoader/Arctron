using System.Text;
using System.Text.Json;
using Jint;
using Jint.Native;
using Jint.Native.Object;

namespace Arctron.Host;

public sealed class JsRuntime
{
    private readonly Engine _engine;

    public JsRuntime(NativeBridge bridge)
    {
        _engine = new Engine(cfg => cfg.AllowClr());

        _engine.SetValue("__arctronNative", bridge);
        _engine.SetValue("console", new ConsoleBridge());
        _engine.SetValue("fetch", new Func<JsValue, JsValue?, Task<string>>(FetchBridge.FetchAsync));

        bridge.TrayOnEvent((trayId, eventName, menuItemId) =>
        {
            try
            {
                var handler = _engine.GetValue("__arctronOnTrayEvent");
                if (handler.IsUndefined() || handler.IsNull())
                {
                    return;
                }

                _engine.Invoke(handler, trayId, eventName, menuItemId ?? JsValue.Undefined);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[arctron] Tray event dispatch error: {ex}");
            }
        });

        bridge.WindowOnRpcInvoke((windowId, requestId, method, argsJson) =>
        {
            try
            {
                var handler = _engine.GetValue("__arctronOnWindowRpcInvoke");
                if (handler.IsUndefined() || handler.IsNull())
                {
                    bridge.WindowRpcReject(windowId, requestId, "Window RPC handler is not registered");
                    return;
                }

                _engine.Invoke(handler, windowId, requestId, method, argsJson);
            }
            catch (Exception ex)
            {
                bridge.WindowRpcReject(windowId, requestId, ex.Message);
                Console.Error.WriteLine($"[arctron] Window RPC dispatch error: {ex}");
            }
        });
    }

    public void RunScript(string code)
    {
        try
        {
            _engine.Execute(code);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[arctron] Jint execution error: {ex}");
        }
    }
}

public sealed class ConsoleBridge
{
    public void log(params object?[] args)
    {
        WriteLine(args, Console.Out);
    }

    public void warn(params object?[] args)
    {
        WriteLine(args, Console.Error);
    }

    public void error(params object?[] args)
    {
        WriteLine(args, Console.Error);
    }

    private static void WriteLine(object?[] args, TextWriter writer)
    {
        if (args.Length == 0)
        {
            writer.WriteLine();
            return;
        }

        writer.WriteLine(string.Join(" ", args));
    }
}

public sealed class FetchResponse
{
    private readonly string _body;

    public FetchResponse(int status, string body)
    {
        Status = status;
        _body = body;
        Ok = status >= 200 && status < 300;
    }

    public bool Ok { get; }
    public int Status { get; }

    public string text()
    {
        return _body;
    }

    public object json()
    {
        return JsonSerializer.Deserialize<object>(_body) ?? new object();
    }
}

public static class FetchBridge
{
    private static readonly HttpClient Client = new();

    public static async Task<string> FetchAsync(JsValue urlValue, JsValue? initValue)
    {
        try
        {
            var url = urlValue.ToString();
            var method = "GET";
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string? body = null;
            int? timeoutMs = null;

            Console.WriteLine($"[arctron] Fetching URL: {url}");

            if (initValue != null && initValue.IsObject())
            {
                var initObj = initValue.AsObject();
                method = ReadString(initObj, "method") ?? method;
                body = ReadString(initObj, "body");
                timeoutMs = ReadInt(initObj, "timeoutMs");

                var headersValue = initObj.Get("headers");
                if (headersValue.IsObject())
                {
                    var headersObj = headersValue.AsObject();
                    foreach (var key in headersObj.GetOwnPropertyKeys())
                    {
                        var name = key.ToString();
                        var value = headersObj.Get(key).ToString();
                        headers[name] = value;
                    }
                }
            }

            using var request = new HttpRequestMessage(new HttpMethod(method), url);
            if (body != null)
            {
                request.Content = new StringContent(body, Encoding.UTF8);
            }

            foreach (var header in headers)
            {
                if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value))
                {
                    request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            using var cts = timeoutMs.HasValue
                ? new CancellationTokenSource(timeoutMs.Value)
                : new CancellationTokenSource();

            using var response = await Client.SendAsync(request, cts.Token).ConfigureAwait(false);
            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            Console.WriteLine($"[arctron] Fetch response: {response.StatusCode}");

            return responseBody;// new FetchResponse((int)response.StatusCode, responseBody);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[arctron] Fetch error: {ex}");
            //return new FetchResponse(0, string.Empty);
            return string.Empty;
        }
    }

    private static string? ReadString(ObjectInstance obj, string name)
    {
        var value = obj.Get(name);
        return value.IsUndefined() || value.IsNull() ? null : value.ToString();
    }

    private static int? ReadInt(ObjectInstance obj, string name)
    {
        var value = obj.Get(name);
        if (value.IsUndefined() || value.IsNull())
        {
            return null;
        }

        return int.TryParse(value.ToString(), out var parsed) ? parsed : null;
    }
}
