namespace OrderIngestion.Common.Exceptions;

public class ApiTimeoutException : ApiException
{
    public ApiTimeoutException()
    {
    }

    public ApiTimeoutException(string message) : base(message)
    {
    }

    public ApiTimeoutException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public ApiTimeoutException(string message, string url) : base(message, url)
    {
    }

    public ApiTimeoutException(string message, string url, Exception innerException) : base(message, url, innerException)
    {
    }
}