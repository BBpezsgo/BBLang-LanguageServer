namespace LanguageServer;

static class Logger
{
    static string FormatMessage(object? message) => message switch
    {
        Exception ex => $"{ex.GetType().Name} {ex.Message}\n{ex.StackTrace}\n\n{FormatMessage(ex.InnerException)}".TrimEnd(),
        null => string.Empty,
        _ => message.ToString() ?? string.Empty,
    };

    public static void Error(string message) => OmniSharpService.Instance?.Server?.Window.LogError(message);
    public static void Warn(string message) => OmniSharpService.Instance?.Server?.Window.LogWarning(message);
    public static void Info(string message) => OmniSharpService.Instance?.Server?.Window.LogInfo(message);
    public static void Debug(string message) => OmniSharpService.Instance?.Server?.Window.LogInfo(message);

    public static void Error(object? message) => Error(FormatMessage(message));
    public static void Warn(object? message) => Warn(FormatMessage(message));
    public static void Info(object? message) => Info(FormatMessage(message));
    public static void Debug(object? message) => Info(FormatMessage(message));
}

