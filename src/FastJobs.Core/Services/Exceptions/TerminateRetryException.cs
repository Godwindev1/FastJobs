// The exception itself — dead simple
public class TerminateRetryException : Exception
{
    public TerminateRetryException(string reason) : base(reason) { }
}