using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Options;

namespace SignalPulse.MarketData.Infrastructure.Elastic;

public sealed class ElasticIndexInitializer(ElasticsearchClient client,
    IOptions<ElasticOptions> options)
{
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        var templateName = $"{options.Value.IndexPrefix}-template";

        var indexPattern = $"{options.Value.IndexPrefix}-*";

        var exists = await client.Indices.ExistsIndexTemplateAsync(templateName, ct);

        if (exists.Exists)
        {
            return;
        }

        var response = await client.Indices.PutIndexTemplateAsync(templateName, t => t
        .IndexPatterns(indexPattern)
        .Template(tmp => tmp
        .Mappings(m => m
        .Properties<WorkflowEventDocument>(p => p
        .Keyword(k => k.CorrelationId)
        .Keyword(k => k.Stage)
        .Keyword(k => k.EventType)
        .Text(t => t.Message)
        .Date(d => d.Timestamp)
        .Flattened("metadata")))), ct);

        if (!response.IsValidResponse)
        {
            throw new InvalidOperationException($"Failed to create Elasticsearch template: {response.DebugInformation}");
        }
    }
}