namespace MarketData.Adapter.Api;

public class BaseUrlConfiguration
{
    public const string CONFIG_NAME = "baseUrls";

    public string ApiBase { get; set; } = default!;
    public string WebBase { get; set; } = default!;
}