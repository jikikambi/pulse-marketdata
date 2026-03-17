using FluentValidation;
using MarketData.Adapter.Shared;
using MarketData.Adapter.Shared.AlphaVantage.Request;
using MarketData.Adapter.Shared.AlphaVantage.Response;
using MarketData.Adapter.Shared.AlphaVantage.Services;
using MarketData.Adapter.Shared.Validation;
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
        services.AddRequestValidators();

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

        var baseAddress = configuration.GetValue<string>("AlphaVantage:BaseAddress") ?? MarketDataConstants.AlphaVantageBaseAddress;

        services.AddRefitClient<IMarketDataAdapterClient>(serializationSettings)
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseAddress));

        // Register ValidatedApiClient
        services.AddTransient<ValidatedApiClient<AlphaVantageQuoteRequest, ApiResponse<AlphaVantageQuoteResponse>>>();
        services.AddTransient<ValidatedApiClient<AlphaVantageForexDailyRequest, ApiResponse<AlphaVantageForexDailyResponse>>>();
    }

    public static IServiceCollection AddRequestValidators(this IServiceCollection services)
    {
        // Registers all validators in the assembly
        services.AddValidatorsFromAssemblyContaining<AlphaVantageQuoteRequestValidator>();
        return services;
    }
}