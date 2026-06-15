using SignalPulse.MarketData.Application.AI.Services.Agents;
using SignalPulse.MarketData.Infrastructure.Elastic;

namespace SignalPulse.MarketAgent.IntegrationTests;

public sealed record EngineTestContext( MarketAgentEngine Engine, IWorkflowEventSink Sink);