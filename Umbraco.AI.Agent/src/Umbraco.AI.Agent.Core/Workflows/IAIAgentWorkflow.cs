using System.Text.Json;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Core.EditableModels;
using MsAIAgent = Microsoft.Agents.AI.AIAgent;

namespace Umbraco.AI.Agent.Core.Workflows;

/// <summary>
/// Interface for agent workflow implementations.
/// </summary>
/// <remarks>
/// <para>
/// Workflows define how orchestrated agents compose multiple agents into a pipeline.
/// Each workflow is a code-based extension point that builds a MAF agent from
/// an agent definition and optional settings.
/// </para>
/// <para>
/// Workflows are discovered via the <see cref="AIAgentWorkflowAttribute"/> and registered
/// in the <see cref="AIAgentWorkflowCollection"/>.
/// </para>
/// </remarks>
public interface IAIAgentWorkflow
{
    /// <summary>
    /// Gets the unique identifier for this workflow.
    /// </summary>
    /// <remarks>
    /// This should be a simple, URL-safe string like "sequential-pipeline" or "round-robin".
    /// </remarks>
    string Id { get; }

    /// <summary>
    /// Gets the display name for this workflow.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets an optional description of what this workflow does.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the settings type for this workflow, or null if no settings are required.
    /// </summary>
    Type? SettingsType { get; }

    /// <summary>
    /// Gets the settings schema for this workflow, or null if no settings are required.
    /// </summary>
    /// <returns>The settings schema, or null.</returns>
    AIEditableModelSchema? GetSettingsSchema();

    /// <summary>
    /// Builds a MAF agent from the workflow definition.
    /// </summary>
    /// <param name="agent">The agent definition containing shared properties like ProfileId.</param>
    /// <param name="settings">Optional workflow-specific settings (JSON).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A MAF agent ready for use with RunAsync/RunStreamingAsync.</returns>
    Task<MsAIAgent> BuildAgentAsync(AIAgent agent, JsonElement? settings, CancellationToken cancellationToken);
}
