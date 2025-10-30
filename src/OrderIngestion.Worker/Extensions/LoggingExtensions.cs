using Microsoft.Extensions.Configuration;
using OrderIngestion.Common.Configuration;
using OrderIngestion.Common.Extensions;
using Serilog;

namespace OrderIngestion.Worker.Extensions;

public static class LoggingExtensions
{
    public static Serilog.ILogger ConfigureLogging(IConfiguration configuration)
    {
        var logPath = configuration.GetRequired<string>(ConfigKeys.Logging.Path);

        return new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File($"{logPath}/worker-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }
}