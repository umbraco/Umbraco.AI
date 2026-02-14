using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Tools.Scopes;

namespace Umbraco.AI.Core.Tools;

/// <summary>
/// Filters tools based on runtime context.
/// </summary>
/// <remarks>
/// Only filters tools that declare ForEntityTypes (context-bound tools).
/// Tools without entity type restrictions (cross-context tools) are always included.
/// </remarks>
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

        // Extract current entity type
        var currentEntityType = runtimeContext.GetValue<string>(Constants.ContextKeys.EntityType);

        // No entity type context = no filtering (return all)
        if (string.IsNullOrEmpty(currentEntityType))
            return toolIds;

        // Filter tools by entity type context
        return toolIds
            .Where(toolId => IsToolRelevantForContext(
                toolId,
                currentEntityType,
                toolCollection,
                scopeCollection))
            .ToList();
    }

    private static bool IsToolRelevantForContext(
        string toolId,
        string currentEntityType,
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

        // Check if tool is context-bound (declares entity types)
        var relevantEntityTypes = scope.ForEntityTypes;
        if (relevantEntityTypes.Count == 0)
        {
            // No entity types declared = cross-context tool = always include
            return true;
        }

        // Context-bound tool: check if current entity type matches
        return relevantEntityTypes.Contains(currentEntityType, StringComparer.OrdinalIgnoreCase);
    }
}
