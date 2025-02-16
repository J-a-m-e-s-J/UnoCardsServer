namespace Exceptions;

public class IpMissingException : Exception
{
    public IpMissingException() {}
    
    public IpMissingException(string message) : base(message) {}
    
    public IpMissingException(string message, Exception innerException) : base(message, innerException) {}
}