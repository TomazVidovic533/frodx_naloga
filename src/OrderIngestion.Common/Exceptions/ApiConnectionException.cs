namespace OrderIngestion.Common.Exceptions;

public class ApiConnectionException : ApiException
{
    public ApiConnectionException()
    {
    }

    public ApiConnectionException(string message) : base(message)
    {
    }

    public ApiConnectionException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public ApiConnectionException(string message, string url) : base(message, url)
    {
    }

    public ApiConnectionException(string message, string url, Exception innerException) : base(message, url, innerException)
    {
    }
}