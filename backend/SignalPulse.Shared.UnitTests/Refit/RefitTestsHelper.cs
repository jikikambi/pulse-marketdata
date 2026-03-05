using Refit;
using System.Net;
using System.Text;
using System.Text.Json;

namespace SignalPulse.Shared.UnitTests.Refit;

public static class RefitTestsHelper
{
    private const string SomethingWentWrongReason = "Something went wrong";

    public static ApiResponse<TContent> CreateOkResponseMock<TContent>(TContent? content)
    {
        return new ApiResponse<TContent>( new HttpResponseMessage()
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(content))
        },
        content,
        new RefitSettings());
    }

    public static ApiResponse<TContent> CreateOkJsonResponseMock<TContent>(string rawJson)
    {
        var http = new HttpResponseMessage()
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(rawJson, Encoding.UTF8, "application/json")
        };

        // Refit normally deserializes via HttpContent.ReadAsStringAsync internally.
        var settings = new RefitSettings()
        {
            ContentSerializer = new SystemTextJsonContentSerializer()
        };

        var deserialized = JsonSerializer.Deserialize<TContent>(rawJson);

        return new ApiResponse<TContent>(http, deserialized, settings);
    }

    public static async Task<ApiException> CreateApiExceptionMockAsync<TContent>(HttpMethod method, TContent? content)
    {
        return await ApiException.Create( SomethingWentWrongReason, new HttpRequestMessage(method, ""),
            method,
            new HttpResponseMessage()
            {
                Content = new StringContent(JsonSerializer.Serialize(content))
            },
            new RefitSettings());
    }
}