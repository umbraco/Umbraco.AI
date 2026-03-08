using Umbraco.AI.Agent.Extensions;
using Umbraco.AI.Core.Tools;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Helper for resolving tool permissions for agents.
/// </summary>
internal static class AIAgentToolHelper
{
    /// <summary>
    /// Computes the list of allowed tool IDs for the specified agent.
    /// Applies user group permission overrides if provided.
    /// </summary>
    /// <param name="agent">The agent.</param>
    /// <param name="toolCollection">The tool collection.</param>
    /// <param name="userGroupIds">Optional user group IDs to apply permission overrides.</param>
    /// <returns>List of allowed tool IDs (deduplicated, case-insensitive).</returns>
    public static IReadOnlyList<string> GetAllowedToolIds(
        AIAgent agent,
        AIToolCollection toolCollection,
        IEnumerable<Guid>? userGroupIds = null)
    {
        ArgumentNullException.ThrowIfNull(agent);
        ArgumentNullException.ThrowIfNull(toolCollection);

        var allowedTools = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var deniedTools = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Always include system tools (cannot be denied)
        var systemToolIds = toolCollection
            .Where(t => t is IAISystemTool)
            .Select(t => t.Id);
        foreach (var toolId in systemToolIds)
        {
            allowedTools.Add(toolId);
        }

        // 2. Get standard config; if not a standard agent, return system tools only
        AIStandardAgentConfig? config = agent.GetStandardConfig();
        if (config is null)
        {
            return allowedTools.ToList();
        }

        // 3. Add agent default tool IDs
        foreach (var toolId in config.AllowedToolIds)
        {
            allowedTools.Add(toolId);
        }

        // 4. Add tools from agent default scopes
        foreach (var scope in config.AllowedToolScopeIds)
        {
            var scopeToolIds = toolCollection.GetByScope(scope)
                .Where(t => t is not IAISystemTool) // Don't duplicate system tools
                .Select(t => t.Id);
            foreach (var toolId in scopeToolIds)
            {
                allowedTools.Add(toolId);
            }
        }

        // 5. Apply user group overrides if provided
        if (userGroupIds is not null)
        {
            foreach (var userGroupId in userGroupIds)
            {
                if (config.UserGroupPermissions.TryGetValue(userGroupId, out var permissions))
                {
                    // 5a. Add allowed tool IDs from user group
                    foreach (var toolId in permissions.AllowedToolIds)
                    {
                        allowedTools.Add(toolId);
                    }

                    // 5b. Add tools from allowed scopes from user group
                    foreach (var scope in permissions.AllowedToolScopeIds)
                    {
                        var scopeToolIds = toolCollection.GetByScope(scope)
                            .Where(t => t is not IAISystemTool)
                            .Select(t => t.Id);
                        foreach (var toolId in scopeToolIds)
                        {
                            allowedTools.Add(toolId);
                        }
                    }

                    // 5c. Track denied tool IDs from user group
                    foreach (var toolId in permissions.DeniedToolIds)
                    {
                        deniedTools.Add(toolId);
                    }

                    // 5d. Track denied tools from denied scopes from user group
                    foreach (var scope in permissions.DeniedToolScopeIds)
                    {
                        var scopeToolIds = toolCollection.GetByScope(scope)
                            .Select(t => t.Id);
                        foreach (var toolId in scopeToolIds)
                        {
                            deniedTools.Add(toolId);
                        }
                    }
                }
            }
        }

        // 6. Remove denied tools (except system tools)
        var systemToolIdSet = new HashSet<string>(systemToolIds, StringComparer.OrdinalIgnoreCase);
        allowedTools.ExceptWith(deniedTools.Where(id => !systemToolIdSet.Contains(id)));

        // 7. Return as list
        return allowedTools.ToList();
    }

    /// <summary>
    /// Checks if a specific tool is allowed for the agent.
    /// Applies user group permission overrides if provided.
    /// </summary>
    /// <param name="agent">The agent.</param>
    /// <param name="toolId">The tool ID to check.</param>
    /// <param name="toolCollection">The tool collection.</param>
    /// <param name="userGroupIds">Optional user group IDs to apply permission overrides.</param>
    /// <returns>True if the tool is allowed, false otherwise.</returns>
    public static bool IsToolAllowed(
        AIAgent agent,
        string toolId,
        AIToolCollection toolCollection,
        IEnumerable<Guid>? userGroupIds = null)
    {
        ArgumentNullException.ThrowIfNull(agent);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolId);
        ArgumentNullException.ThrowIfNull(toolCollection);

        // Non-standard agents have no tool permissions beyond system tools
        if (agent.GetStandardConfig() is null)
        {
            return toolCollection.Any(t => t is IAISystemTool && string.Equals(t.Id, toolId, StringComparison.OrdinalIgnoreCase));
        }

        // Get all allowed tool IDs (includes user group overrides if provided)
        var allowedToolIds = GetAllowedToolIds(agent, toolCollection, userGroupIds);

        // Check if the tool ID is in the allowed list
        return allowedToolIds.Contains(toolId, StringComparer.OrdinalIgnoreCase);
    }
}
