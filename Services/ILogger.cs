namespace Services;

public interface ILogger
{
    void Write(LogMessageType messageType, string message);
}
