namespace LanguageServer;

static class Logger
{
    public static void Error(string message) => OmniSharpService.Instance?.Server?.Window.LogError(message);
    public static void Warn(string message) => OmniSharpService.Instance?.Server?.Window.LogWarning(message);
    public static void Info(string message) => OmniSharpService.Instance?.Server?.Window.LogInfo(message);
    public static void Debug(string message) => OmniSharpService.Instance?.Server?.Window.LogInfo(message);

    public static void Error(object? message) => Error(message?.ToString() ?? string.Empty);
    public static void Warn(object? message) => Warn(message?.ToString() ?? string.Empty);
    public static void Info(object? message) => Info(message?.ToString() ?? string.Empty);
    public static void Debug(object? message) => Info(message?.ToString() ?? string.Empty);
}

