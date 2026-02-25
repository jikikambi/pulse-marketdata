using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace MarketData.Adapter.Api.Client;

[ExcludeFromCodeCoverage(Justification = "No logic")]
public static class ServiceCollectionExtensions
{
    public static void AddAlphaVantageApi(this IServiceCollection services, IConfiguration configuration)
    {
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null, // PascalCase is the default for System.Text.Json
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var serializationSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(jsonSerializerOptions)
        };

        var baseAddress = configuration.GetValue<string>("AlphaVantage:BaseAddress") ?? "https://www.alphavantage.co";

        services.AddRefitClient<IMarketDataAdapterClient>(serializationSettings)
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseAddress));
    }
}