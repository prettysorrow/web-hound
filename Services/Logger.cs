namespace Services;

public class Logger : ILogger
{
    public void Write(LogMessageType messageType, string message)
    {
        var tag = messageType switch
        {
            LogMessageType.Fatal => "[fatal]",
            LogMessageType.Info => $"[info]",
            LogMessageType.Warning => $"[warning]",
            _ => throw new ShouldNotHappenException()
        };

        var channel = messageType switch
        {
            LogMessageType.Info => Console.Out,
            LogMessageType.Fatal => Console.Error,
            LogMessageType.Warning => Console.Error,
            _ => throw new ShouldNotHappenException()
        };

        Console.WriteLine(message, channel);
    }
}
