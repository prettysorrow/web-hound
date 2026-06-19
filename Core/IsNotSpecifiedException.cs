namespace Core;

public class IsNotSpecifiedException : Exception
{
    public IsNotSpecifiedException(string parameter)
        : base(message: $"parameter {parameter} is not specified")
    {
    }
}
