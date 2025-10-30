namespace OrderIngestion.Application.Models;

public class OrderIngestionResult
{
    public int SavedCount { get; set; }
    public int DuplicateCount { get; set; }
    public int TotalOrdersInDatabase { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }

    public override string ToString()
    {
        return IsSuccess
            ? $"Saved: {SavedCount}, Duplicates: {DuplicateCount}, Total in DB: {TotalOrdersInDatabase}"
            : $"Failed: {ErrorMessage}";
    }
}