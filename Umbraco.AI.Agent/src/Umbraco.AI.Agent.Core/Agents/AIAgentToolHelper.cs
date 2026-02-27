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

        // 2. Add agent default tool IDs
        foreach (var toolId in agent.AllowedToolIds)
        {
            allowedTools.Add(toolId);
        }

        // 3. Add tools from agent default scopes
        foreach (var scope in agent.AllowedToolScopeIds)
        {
            var scopeToolIds = toolCollection.GetByScope(scope)
                .Where(t => t is not IAISystemTool) // Don't duplicate system tools
                .Select(t => t.Id);
            foreach (var toolId in scopeToolIds)
            {
                allowedTools.Add(toolId);
            }
        }

        // 4. Apply user group overrides if provided
        if (userGroupIds is not null)
        {
            foreach (var userGroupId in userGroupIds)
            {
                if (agent.UserGroupPermissions.TryGetValue(userGroupId, out var permissions))
                {
                    // 4a. Add allowed tool IDs from user group
                    foreach (var toolId in permissions.AllowedToolIds)
                    {
                        allowedTools.Add(toolId);
                    }

                    // 4b. Add tools from allowed scopes from user group
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

                    // 4c. Track denied tool IDs from user group
                    foreach (var toolId in permissions.DeniedToolIds)
                    {
                        deniedTools.Add(toolId);
                    }

                    // 4d. Track denied tools from denied scopes from user group
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

        // 5. Remove denied tools (except system tools)
        var systemToolIdSet = new HashSet<string>(systemToolIds, StringComparer.OrdinalIgnoreCase);
        allowedTools.ExceptWith(deniedTools.Where(id => !systemToolIdSet.Contains(id)));

        // 6. Return as list
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

        // Get all allowed tool IDs (includes user group overrides if provided)
        var allowedToolIds = GetAllowedToolIds(agent, toolCollection, userGroupIds);

        // Check if the tool ID is in the allowed list
        return allowedToolIds.Contains(toolId, StringComparer.OrdinalIgnoreCase);
    }
}
