using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SignalPulse.MarketData.Infrastructure.Elastic;

public sealed class ElasticWorkflowEventSink(ElasticsearchClient client,
    IOptions<ElasticOptions> options,
    ILogger<ElasticWorkflowEventSink> logger)
    : IWorkflowEventSink
{
    public async Task WriteAsync(WorkflowEvent evt, CancellationToken ct)
    {
        try
        {
            var indexName = $"{options.Value.IndexPrefix}-{DateTime.UtcNow:yyyy.MM.dd}";

            var exists = await client.Indices.ExistsAsync(indexName, ct);

            if (exists.Exists)
            {
                return;
            }

            var doc = new WorkflowEventDocument
            {
                CorrelationId = evt.CorrelationId,
                Stage = evt.Stage,
                EventType = evt.EventType,
                Message = evt.Message,
                Timestamp = evt.Timestamp,
                Metadata = evt.Metadata
            };

            var response = await client.IndexAsync(doc, i => i.Index(indexName), ct);

            if (!response.IsValidResponse)
            {
                logger.LogError("Failed to index workflow event. Error: {Error}", response.ElasticsearchServerError);
            }
            else
            {
                logger.LogInformation("Indexed workflow event into {Index}", indexName);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to index workflow event for {CorrelationId}", evt.CorrelationId);
        }
    }
}
