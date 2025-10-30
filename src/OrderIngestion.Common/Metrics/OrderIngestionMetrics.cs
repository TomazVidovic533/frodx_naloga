using System.Diagnostics.Metrics;

namespace OrderIngestion.Common.Metrics;

public static class OrderIngestionMetrics
{
    private static readonly Meter Meter = new("OrderIngestion.Metrics");

    public static readonly Counter<long> OrdersExtracted = Meter.CreateCounter<long>(
        "orderingestion_orders_extracted_total",
        description: "Total number of orders extracted from API");

    public static readonly Counter<long> OrdersSaved = Meter.CreateCounter<long>(
        "orderingestion_orders_saved_total",
        description: "Total number of orders saved to database");

    public static readonly Counter<long> OrdersDuplicate = Meter.CreateCounter<long>(
        "orderingestion_orders_duplicate_total",
        description: "Total number of duplicate orders skipped");
}