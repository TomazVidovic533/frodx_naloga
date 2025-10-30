using System.Net;

namespace OrderIngestion.Common.Exceptions;

public class ApiResponseException : ApiException
{
    public HttpStatusCode? StatusCode { get; set; }

    public ApiResponseException()
    {
    }

    public ApiResponseException(string message) : base(message)
    {
    }

    public ApiResponseException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public ApiResponseException(string message, HttpStatusCode statusCode) : base(message)
    {
        StatusCode = statusCode;
    }

    public ApiResponseException(string message, string url, HttpStatusCode statusCode) : base(message, url)
    {
        StatusCode = statusCode;
    }

    public ApiResponseException(string message, string url, HttpStatusCode statusCode, Exception innerException)
        : base(message, url, innerException)
    {
        StatusCode = statusCode;
    }
}