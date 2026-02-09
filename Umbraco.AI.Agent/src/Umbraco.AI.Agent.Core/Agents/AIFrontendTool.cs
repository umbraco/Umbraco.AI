using Umbraco.AI.AGUI.Models;

namespace Umbraco.AI.Agent.Core.Agents;

/// <summary>
/// Frontend tool with metadata for permission filtering.
/// Combines the AG-UI tool definition with Umbraco-specific metadata (scope and destructiveness).
/// </summary>
/// <param name="Tool">The AG-UI tool definition for the LLM.</param>
/// <param name="Scope">Tool scope for permission grouping (e.g., 'entity-write', 'navigation').</param>
/// <param name="IsDestructive">Whether the tool performs destructive operations (e.g., delete, publish).</param>
public record AIFrontendTool(
    AGUITool Tool,
    string? Scope,
    bool IsDestructive);
