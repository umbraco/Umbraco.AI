using Umbraco.AI.Core.Tools;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Helper for resolving tool permissions for agents.
/// </summary>
internal static class AIAgentToolHelper
{
    /// <summary>
    /// Computes the list of allowed tool IDs for the specified agent.
    /// </summary>
    /// <param name="agent">The agent.</param>
    /// <param name="toolCollection">The tool collection.</param>
    /// <returns>List of allowed tool IDs (deduplicated, case-insensitive).</returns>
    public static IReadOnlyList<string> GetAllowedToolIds(
        AIAgent agent,
        AIToolCollection toolCollection)
    {
        ArgumentNullException.ThrowIfNull(agent);
        ArgumentNullException.ThrowIfNull(toolCollection);

        var allowedTools = new List<string>();

        // 1. Always include system tools
        var systemToolIds = toolCollection
            .Where(t => t is IAISystemTool)
            .Select(t => t.Id);
        allowedTools.AddRange(systemToolIds);

        // 2. Add explicitly allowed tool IDs
        if (agent.AllowedToolIds.Count > 0)
        {
            allowedTools.AddRange(agent.AllowedToolIds);
        }

        // 3. Add tools from allowed scopes
        if (agent.AllowedToolScopeIds.Count > 0)
        {
            foreach (var scope in agent.AllowedToolScopeIds)
            {
                var scopeToolIds = toolCollection.GetByScope(scope)
                    .Where(t => t is not IAISystemTool) // Don't duplicate system tools
                    .Select(t => t.Id);
                allowedTools.AddRange(scopeToolIds);
            }
        }

        // 4. Deduplicate and return
        return allowedTools
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Checks if a specific tool is allowed for the agent.
    /// </summary>
    /// <param name="agent">The agent.</param>
    /// <param name="toolId">The tool ID to check.</param>
    /// <param name="toolCollection">The tool collection.</param>
    /// <returns>True if the tool is allowed, false otherwise.</returns>
    public static bool IsToolAllowed(
        AIAgent agent,
        string toolId,
        AIToolCollection toolCollection)
    {
        ArgumentNullException.ThrowIfNull(agent);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolId);
        ArgumentNullException.ThrowIfNull(toolCollection);

        // Check if it's a system tool (always allowed)
        var tool = toolCollection.FirstOrDefault(t =>
            t.Id.Equals(toolId, StringComparison.OrdinalIgnoreCase));

        if (tool is IAISystemTool)
        {
            return true;
        }

        // Check if explicitly allowed by ID
        if (agent.AllowedToolIds.Contains(toolId, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check if tool's scope is allowed
        if (tool?.ScopeId is not null &&
            agent.AllowedToolScopeIds.Contains(tool.ScopeId, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
