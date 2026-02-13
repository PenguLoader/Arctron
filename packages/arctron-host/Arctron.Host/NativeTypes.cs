namespace Arctron.Host;

public sealed class WindowOptions
{
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? X { get; set; }
    public int? Y { get; set; }
    public string? Title { get; set; }
    public bool? Show { get; set; }
    public bool? Frameless { get; set; }
    public string? InitialUrl { get; set; }
    public bool? DevTools { get; set; }
    public bool? ContextMenu { get; set; }
    public string? RpcNamespace { get; set; }
    public string[]? RpcMethods { get; set; }
}

public sealed class TrayMenuItem
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool? Enabled { get; set; }
}

public sealed class TrayOptions
{
    public string? Tooltip { get; set; }
    public string? Icon { get; set; }
    public TrayMenuItem[]? Menu { get; set; }
}

public sealed class OpenDialogOptions
{
    public string? Title { get; set; }
    public bool? AllowMultiple { get; set; }
    public DialogFilter[]? Filters { get; set; }
}

public sealed class SaveDialogOptions
{
    public string? Title { get; set; }
    public string? DefaultPath { get; set; }
    public DialogFilter[]? Filters { get; set; }
}

public sealed class DialogFilter
{
    public string Name { get; set; } = string.Empty;
    public string[] Extensions { get; set; } = Array.Empty<string>();
}

public sealed class MessageBoxOptions
{
    public string? Title { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public string[]? Buttons { get; set; }
}

public sealed class MessageBoxResult
{
    public int Response { get; set; }
}

public sealed class ExecOptions
{
    public string? Cwd { get; set; }
    public Dictionary<string, string>? Env { get; set; }
    public int? TimeoutMs { get; set; }
}

public sealed class ExecResult
{
    public int ExitCode { get; set; }
    public string Stdout { get; set; } = string.Empty;
    public string Stderr { get; set; } = string.Empty;
}
