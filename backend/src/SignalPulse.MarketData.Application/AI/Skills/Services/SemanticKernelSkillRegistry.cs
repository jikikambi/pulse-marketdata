using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace SignalPulse.MarketData.Application.AI.Skills.Services;

public sealed class SemanticKernelSkillRegistry : ISkillRegistry
{
    private readonly Dictionary<string, KernelFunction> _skills = [];
    private readonly ILogger<SemanticKernelSkillRegistry> _logger;

    public SemanticKernelSkillRegistry(Kernel kernel,
        ILogger<SemanticKernelSkillRegistry> logger)
    {
        _logger = logger;
        var skillsPath = Path.Combine(AppContext.BaseDirectory, AgentConstants.SkillsPath);

        LoadSkill(kernel, skillsPath, AgentConstants.PlannerSkill);
        LoadSkill(kernel, skillsPath, AgentConstants.ReasonerSkill);
    }

    private void LoadSkill(Kernel kernel, string basePath, string skillName)
    {
        var plugin = kernel.CreatePluginFromPromptDirectory(Path.Combine(basePath, skillName));

        foreach (var function in plugin)
        {
            _logger.LogInformation( $"Plugin={skillName}, Function={function.Name}");

            _skills[function.Name] = function;
        }
    }

    public KernelFunction Get(string skillName) => _skills[skillName];
}