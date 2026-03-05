using FluentAssertions;
using MarketData.Adapter.Api.Client.AlphaVantage;
using MarketData.Adapter.Shared.AlphaVantage.Request;
using Refit;
using System.Net;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace MarketData.Adapter.Api.Client.IntegrationTests;

public class AlphaVantageApiWireMockTests : IDisposable
{
    private readonly WireMockServer _server;

    public AlphaVantageApiWireMockTests()
    {
        _server = WireMockServer.Start();
    }

    [Fact]
    public async Task GetQuoteAsync_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = """
        {
          "Global Quote": {
            "01. symbol": "MSFT",
            "05. price": "336.3200"
          }
        }
        """;

        _server.Given(Request.Create().WithPath("/query").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody(json));

        var api = RestService.For<IAlphaVantageApi>(
            new HttpClient { BaseAddress = new Uri(_server.Url!) },
            new RefitSettings { ContentSerializer = new SystemTextJsonContentSerializer() }
        );

        var req = new AlphaVantageQuoteRequest
        {
            Function = "GLOBAL_QUOTE",
            Symbol = "MSFT",
            Apikey = "demo"
        };

        // Act
        var result = await api.GetQuoteAsync(req, CancellationToken.None);

        // Assert
        result!.Content.Should().NotBeNull();
        result.Content!.Quote.Should().NotBeNull();
        result.Content!.Quote!.Price.Should().Be("336.3200");
    }

    [Fact]
    public async Task GetQuoteAsync_WhenBadRequest_ShouldReturnApiResponseWithError()
    {
        // Arrange
        _server.Given(Request.Create().WithPath("/query").UsingGet())
            .RespondWith(Response.Create()
            .WithStatusCode(400)
            .WithBody("Bad request"));

        var api = RestService.For<IAlphaVantageApi>(new HttpClient { BaseAddress = new Uri(_server.Url!) });

        var req = new AlphaVantageQuoteRequest
        {
            Function = "GLOBAL_QUOTE",
            Symbol = "MSFT",
            Apikey = "demo"
        };

        // Act
        var response = await api.GetQuoteAsync(req, CancellationToken.None);

        // Assert
        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetQuoteAsync_WhenServerError_ShouldReturn500()
    {
        // Arrange
        _server.Given(Request.Create().WithPath("/query").UsingGet())
            .RespondWith(Response.Create()
            .WithStatusCode(500)
            .WithBody("Internal server error"));
        
        var api = RestService.For<IAlphaVantageApi>(new HttpClient { BaseAddress = new Uri(_server.Url!) });

        var req = new AlphaVantageQuoteRequest
        {
            Function = "GLOBAL_QUOTE",
            Symbol = "MSFT",
            Apikey = "demo"
        };

        // Act
        var response = await api.GetQuoteAsync(req, CancellationToken.None);

        // Assert
        response.IsSuccessStatusCode.Should().BeFalse();
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetQuoteAsync_WhenThrottled_ShouldReturnEmptyQuote()
    {
        // Arrange
        var json = """
        {         
            "Note": "Thank you for using Alpha Vantage! Our standard API call frequency is 5 calls per minute."
        }
        """;

        _server.Given(Request.Create().WithPath("/query").UsingGet())
            .RespondWith(Response.Create()
            .WithStatusCode(200)
            .WithBody(json));

        var api = RestService.For<IAlphaVantageApi>(new HttpClient { BaseAddress = new Uri(_server.Url!) });

        var req = new AlphaVantageQuoteRequest
        {
            Function = "GLOBAL_QUOTE",
            Symbol = "MSFT",
            Apikey = "demo"
        };

        // Act
        var response = await api.GetQuoteAsync(req, CancellationToken.None);

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content!.Quote.Should().BeNull();
    }

    [Fact]
    public async Task GetQuoteAsync_WhenRateLimited_ShouldReturn429()
    {
        // Arrange
        _server.Given(Request.Create().WithPath("/query").UsingGet())
            .RespondWith(Response.Create()
            .WithStatusCode(429)
            .WithBody("Too many requests"));

        var api = RestService.For<IAlphaVantageApi>(new HttpClient { BaseAddress = new Uri(_server.Url!) });

        var req = new AlphaVantageQuoteRequest
        {
            Function = "GLOBAL_QUOTE",
            Symbol = "MSFT",
            Apikey = "demo"
        };

        // Act
        var response = await api.GetQuoteAsync(req, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be((HttpStatusCode)429);
    }

    [Fact]
    public async Task GetQuoteAsync_WhenQuoteEmpty_ShouldReturnEmptyQuoteObject()
    {
        // Arrange
        var json = """
        {
           "Global Quote": { }
        }
        """;

        _server.Given(Request.Create().WithPath("/query").UsingGet())
            .RespondWith(Response.Create()
            .WithStatusCode(200)
            .WithBody(json));

        var api = RestService.For<IAlphaVantageApi>(new HttpClient { BaseAddress = new Uri(_server.Url!) });

        var req = new AlphaVantageQuoteRequest
        {
            Function = "GLOBAL_QUOTE",
            Symbol = "MSFT",
            Apikey = "demo"
        };

        // Act
        var response = await api.GetQuoteAsync(req, CancellationToken.None);

        // Assert
        response.Content.Should().NotBeNull();
        response.Content!.Quote.Should().NotBeNull();
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }
}