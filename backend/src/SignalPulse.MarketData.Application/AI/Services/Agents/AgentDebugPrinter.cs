using SignalPulse.MarketData.Application.AI.Models;

namespace SignalPulse.MarketData.Application.AI.Services.Agents;

public static class AgentDebugPrinter
{
    public static void Print(AgentReplayResult replay)
    {
        Console.WriteLine($"=== AGENT SESSION: {replay.Key} ===");

        foreach (var step in replay.Timeline)
        {
            Console.WriteLine(step);
            Console.WriteLine("----------------------------------");
        }

        Console.WriteLine($"Completed: {replay.Completed}");
    }
}