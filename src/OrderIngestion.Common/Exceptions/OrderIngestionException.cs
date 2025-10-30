namespace OrderIngestion.Common.Exceptions;

public class OrderIngestionException : Exception
{
    public OrderIngestionException()
    {
    }

    public OrderIngestionException(string message) : base(message)
    {
    }

    public OrderIngestionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}