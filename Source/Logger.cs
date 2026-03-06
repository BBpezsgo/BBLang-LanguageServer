namespace LanguageServer;

static class Logger
{
    static string FormatMessage(object? message) => message switch
    {
        Exception ex => $"{ex.GetType().Name} {ex.Message}\n{ex.StackTrace}\n\n{FormatMessage(ex.InnerException)}".TrimEnd(),
        null => string.Empty,
        _ => message.ToString() ?? string.Empty,
    };

    public static void Error(string message) => OmniSharpService.Instance?.Server?.Window.SendNotification(new LogMessageParams { Type = (MessageType)1, Message = message });
    public static void Warn(string message) => OmniSharpService.Instance?.Server?.Window.SendNotification(new LogMessageParams { Type = (MessageType)2, Message = message });
    public static void Info(string message) => OmniSharpService.Instance?.Server?.Window.SendNotification(new LogMessageParams { Type = (MessageType)3, Message = message });
    public static void Debug(string message) => OmniSharpService.Instance?.Server?.Window.SendNotification(new LogMessageParams { Type = (MessageType)5, Message = message });
    public static void Trace(string message) => OmniSharpService.Instance?.Server?.Window.SendNotification(new LogMessageParams { Type = (MessageType)6, Message = message });

    public static void Error(object? message) => Error(FormatMessage(message));
    public static void Warn(object? message) => Warn(FormatMessage(message));
    public static void Info(object? message) => Info(FormatMessage(message));
    public static void Debug(object? message) => Debug(FormatMessage(message));
    public static void Trace(object? message) => Trace(FormatMessage(message));
}

