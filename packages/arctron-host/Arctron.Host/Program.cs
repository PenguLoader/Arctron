using System;
using System.IO;

namespace Arctron.Host;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        var mainPath = ResolveMainPath(args);
        if (string.IsNullOrWhiteSpace(mainPath))
        {
            Console.Error.WriteLine("Missing main script. Set ARCTRON_MAIN or pass path as first argument.");
            return 1;
        }

        var script = File.ReadAllText(mainPath);
        var appHost = new AppHost(script);

        return appHost.Run();
    }

    private static string? ResolveMainPath(string[] args)
    {
        if (args.Length > 0 && File.Exists(args[0]))
        {
            return Path.GetFullPath(args[0]);
        }

        var env = Environment.GetEnvironmentVariable("ARCTRON_MAIN");
        if (!string.IsNullOrWhiteSpace(env) && File.Exists(env))
        {
            return Path.GetFullPath(env);
        }

        return null;
    }
}
