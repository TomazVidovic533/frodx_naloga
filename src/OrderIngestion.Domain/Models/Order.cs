namespace OrderIngestion.Domain.Models;

public class Order
{
    public int Id { get; set; }
    public int ExternalId { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool Completed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}