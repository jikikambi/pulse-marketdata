using Microsoft.SemanticKernel;

namespace SignalPulse.MarketData.Application.AI.Skills.Services;

public sealed class SemanticKernelSkillRegistry : ISkillRegistry
{
    private readonly Dictionary<string, KernelFunction> _skills = [];

    public SemanticKernelSkillRegistry(Kernel kernel)
    {
        var skillsPath = Path.Combine( AppContext.BaseDirectory, AgentConstants.SkillsPath);

        LoadSkill(kernel, skillsPath, AgentConstants.PlannerSkill);
        LoadSkill(kernel, skillsPath, AgentConstants.ReasonerSkill);
    }

    private void LoadSkill(Kernel kernel, string basePath, string skillName)
    {
        var plugin = kernel.CreatePluginFromPromptDirectory( Path.Combine(basePath, skillName));

        foreach (var function in plugin)
        {
            _skills[function.Name] = function;
        }
    }

    public KernelFunction Get(string skillName) => _skills[skillName];
}