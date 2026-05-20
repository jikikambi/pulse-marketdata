using Microsoft.SemanticKernel;

namespace SignalPulse.MarketData.Application.AI.Skills.Services;

public interface ISkillRegistry
{
    KernelFunction Get(string skillName);
}