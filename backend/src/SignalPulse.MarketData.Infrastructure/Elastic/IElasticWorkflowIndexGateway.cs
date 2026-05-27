using Elastic.Clients.Elasticsearch;

namespace SignalPulse.MarketData.Infrastructure.Elastic;

public interface IElasticWorkflowIndexGateway
{
    Task<bool> IndexExistsAsync(string indexName, CancellationToken ct);
    Task<IndexResponse> IndexAsync(WorkflowEventDocument doc, string indexName, CancellationToken ct);
}
