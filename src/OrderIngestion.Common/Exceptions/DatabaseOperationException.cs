namespace OrderIngestion.Common.Exceptions;

public class DatabaseOperationException : DataException
{
    public string? Operation { get; set; }

    public DatabaseOperationException()
    {
    }

    public DatabaseOperationException(string message) : base(message)
    {
    }

    public DatabaseOperationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public DatabaseOperationException(string message, string operation) : base(message)
    {
        Operation = operation;
    }

    public DatabaseOperationException(string message, string operation, Exception innerException) : base(message, innerException)
    {
        Operation = operation;
    }
}