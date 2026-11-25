namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Finds which groups' tool rules differ between a saved agent and the one being saved.
/// </summary>
internal static class AIAgentToolRuleChanges
{
    /// <summary>
    /// Returns the groups whose entry was added, removed or changed. A config without tool rules
    /// (null, or not a standard config) counts as having none.
    /// </summary>
    public static IReadOnlyCollection<Guid> GetChangedGroupKeys(IAIAgentConfig? existing, IAIAgentConfig? requested)
    {
        var before = GetRules(existing);
        var after = GetRules(requested);

        return before.Keys.Union(after.Keys)
            .Where(key => !(before.TryGetValue(key, out var b) && after.TryGetValue(key, out var a) && AreEqual(b, a)))
            .ToArray();
    }

    private static IReadOnlyDictionary<Guid, AIAgentUserGroupPermissions> GetRules(IAIAgentConfig? config)
        => (config as AIStandardAgentConfig)?.UserGroupPermissions
           ?? new Dictionary<Guid, AIAgentUserGroupPermissions>();

    // The four lists are allow/deny sets, so order and duplicates don't matter. Tool and scope ids
    // are compared case-insensitively, as the permission checks do.
    private static bool AreEqual(AIAgentUserGroupPermissions a, AIAgentUserGroupPermissions b)
        => SameSet(a.AllowedToolIds, b.AllowedToolIds)
           && SameSet(a.AllowedToolScopeIds, b.AllowedToolScopeIds)
           && SameSet(a.DeniedToolIds, b.DeniedToolIds)
           && SameSet(a.DeniedToolScopeIds, b.DeniedToolScopeIds);

    private static bool SameSet(IReadOnlyList<string> a, IReadOnlyList<string> b)
        => a.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(b);
}
