using OrderIngestion.Domain.Models;

namespace OrderIngestion.Application.Models;

public class OrderBatch
{
    public DateTime ExtractedAt { get; set; }
    public string BatchId { get; set; } = string.Empty;
    public List<Order> Orders { get; set; } = new();
}