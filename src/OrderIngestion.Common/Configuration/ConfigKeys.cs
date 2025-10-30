namespace OrderIngestion.Common.Configuration;

public static class ConfigKeys
{
    public static class Logging
    {
        public const string Path = "Logging:Path";
    }

    public static class ConnectionStrings
    {
        public const string OrderDb = "ConnectionStrings:OrderDb";
    }

    public static class ApiSettings
    {
        public const string BaseUrl = "ApiSettings:BaseUrl";
    }

    public static class RetryPolicy
    {
        public const string RetryCount = "RetryPolicy:RetryCount";
    }

    public static class MockApi
    {
        public const string EnableRandomErrors = "MockApi:EnableRandomErrors";
        public const string ErrorRate = "MockApi:ErrorRate";
        public const string MinDelayMs = "MockApi:MinDelayMs";
        public const string MaxDelayMs = "MockApi:MaxDelayMs";
    }

    public static class Ingestion
    {
        public const string IntervalSeconds = "IngestionIntervalSeconds";
        public const string DownloadPath = "Ingestion:DownloadPath";
    }
}