using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Tools.Scopes;

namespace Umbraco.AI.Core.Tools;

/// <summary>
/// Filters tools based on runtime context.
/// </summary>
public static class AIToolContextFilter
{
    /// <summary>
    /// Filters tool IDs to only those relevant for the current runtime context.
    /// </summary>
    /// <param name="toolIds">The allowed tool IDs (already permission-filtered).</param>
    /// <param name="runtimeContext">The current runtime context.</param>
    /// <param name="toolCollection">The tool collection.</param>
    /// <param name="scopeCollection">The tool scope collection.</param>
    /// <returns>Filtered tool IDs relevant for current context.</returns>
    public static IReadOnlyList<string> FilterByContext(
        IReadOnlyList<string> toolIds,
        AIRuntimeContext? runtimeContext,
        AIToolCollection toolCollection,
        AIToolScopeCollection scopeCollection)
    {
        // No runtime context = no filtering (return all)
        if (runtimeContext == null)
            return toolIds;

        // Extract context values
        var currentEntityType = runtimeContext.GetValue<string>(Constants.ContextKeys.EntityType);
        var currentSection = runtimeContext.GetValue<string>(Constants.ContextKeys.SectionAlias);

        // No context values = no filtering (return all)
        if (string.IsNullOrEmpty(currentEntityType) && string.IsNullOrEmpty(currentSection))
            return toolIds;

        // Filter tools by context
        return toolIds
            .Where(toolId => IsToolRelevantForContext(
                toolId,
                currentEntityType,
                currentSection,
                toolCollection,
                scopeCollection))
            .ToList();
    }

    private static bool IsToolRelevantForContext(
        string toolId,
        string? currentEntityType,
        string? currentSection,
        AIToolCollection toolCollection,
        AIToolScopeCollection scopeCollection)
    {
        var tool = toolCollection.GetById(toolId);
        if (tool == null)
            return false; // Unknown tool = exclude

        // System tools always included
        if (tool is IAISystemTool)
            return true;

        var scope = scopeCollection.GetById(tool.ScopeId);
        if (scope == null)
            return true; // Unknown scope = include (backwards compatible)

        // Check entity type filtering
        var relevantEntityTypes = scope.ForEntityTypes;
        if (relevantEntityTypes.Count > 0)
        {
            // Scope declares entity types = check if current context matches
            if (string.IsNullOrEmpty(currentEntityType))
            {
                // No entity context but scope requires it = exclude
                return false;
            }

            // Check if current entity type is in the relevant list
            if (!relevantEntityTypes.Contains(currentEntityType, StringComparer.OrdinalIgnoreCase))
            {
                return false; // Entity type mismatch
            }
        }

        // All checks passed
        return true;
    }
}
