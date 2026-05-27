using Elastic.Clients.Elasticsearch;

namespace SignalPulse.MarketData.Infrastructure.Elastic;

public sealed class ElasticWorkflowIndexGateway(ElasticsearchClient client)
    : IElasticWorkflowIndexGateway
{
    public async Task<bool> IndexExistsAsync(string indexName, CancellationToken ct)
    {
        var response = await client.Indices.ExistsAsync(indexName, ct);
        return response.Exists;
    }

    public Task<IndexResponse> IndexAsync(WorkflowEventDocument doc, string indexName, CancellationToken ct) => client.IndexAsync(doc, i => i.Index(indexName), ct);
}