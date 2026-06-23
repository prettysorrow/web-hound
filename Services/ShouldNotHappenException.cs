namespace Services;

public class ShouldNotHappenException : Exception
{
    public ShouldNotHappenException() : base()
    {
    }

    public ShouldNotHappenException(string message) : base(message)
    {
    }
}
