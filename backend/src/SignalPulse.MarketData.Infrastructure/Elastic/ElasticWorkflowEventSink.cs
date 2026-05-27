using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using SignalPulse.MarketData.Infrastructure.Policies;

namespace SignalPulse.MarketData.Infrastructure.Elastic;

public sealed class ElasticWorkflowEventSink(IElasticWorkflowIndexGateway gateway,
    IOptions<ElasticOptions> options,
    IAiPolicyRegistry policyRegistry,
    ILogger<ElasticWorkflowEventSink> logger)
    : IWorkflowEventSink
{
    private readonly IAsyncPolicy _policy = policyRegistry.GetElasticPolicy();

    public async Task WriteAsync(WorkflowEvent evt, CancellationToken ct)
    {
        try
        {
            var indexName = $"{options.Value.IndexPrefix}-{DateTime.UtcNow:yyyy.MM.dd}";

            if (await gateway.IndexExistsAsync(indexName, ct)) return;

            var doc = new WorkflowEventDocument
            {
                CorrelationId = evt.CorrelationId,
                Stage = evt.Stage,
                EventType = evt.EventType,
                Message = evt.Message,
                Timestamp = evt.Timestamp,
                Metadata = evt.Metadata
            };

            await _policy.ExecuteAsync(async token =>
            {

                var response = await gateway.IndexAsync(doc, indexName, token);

                if (!response.IsValidResponse)
                {
                    logger.LogError("Failed to index workflow event. Error: {Error}", response.ElasticsearchServerError);
                }
                else
                {
                    logger.LogInformation("Indexed workflow event into {Index}", indexName);
                }
            }, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to index workflow event for {CorrelationId}", evt.CorrelationId);
        }
    }
}
