namespace OrderIngestion.Common.Exceptions;

public class ApiException : OrderIngestionException
{
    public string? Url { get; set; }

    public ApiException()
    {
    }

    public ApiException(string message) : base(message)
    {
    }

    public ApiException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public ApiException(string message, string url) : base(message)
    {
        Url = url;
    }

    public ApiException(string message, string url, Exception innerException) : base(message, innerException)
    {
        Url = url;
    }
}