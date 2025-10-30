using Microsoft.Extensions.Configuration;

namespace OrderIngestion.Common.Extensions;

public static class ConfigurationExtensions
{
    public static T GetRequired<T>(this IConfiguration configuration, string key)
    {
        var value = configuration.GetValue<T>(key);

        if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
        {
            throw new InvalidOperationException(
                $"Required configuration '{key}' is missing or empty. " +
                $"Ensure '{key.Replace(":", "__")}' is set in environment variables.");
        }

        return value;
    }
}