using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Agent.Core.Chat;

/// <summary>
/// Factory for creating MAF AIAgent instances from agent definitions.
/// </summary>
/// <remarks>
/// <para>
/// This factory creates <see cref="ChatClientAgent"/> instances that wrap the Umbraco.Ai
/// chat pipeline. The created agents can be used directly with MAF's <c>RunAsync</c> and
/// <c>RunStreamingAsync</c> methods for both automation scenarios and AG-UI streaming.
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// </para>
/// <code>
/// // Get agent definition
/// var agentDef = await _agentService.GetAgentAsync(agentId, ct);
///
/// // Create MAF agent
/// var agent = await _agentFactory.CreateAgentAsync(agentDef, ct);
///
/// // Use MAF API
/// var response = await agent.RunAsync("Do something");
/// // or streaming
/// await foreach (var update in agent.RunStreamingAsync("Do something")) { }
/// </code>
/// </remarks>
public interface IAIAgentFactory
{
    /// <summary>
    /// Creates an AIAgent from an agent definition with automatic runtime context management.
    /// </summary>
    /// <param name="agent">The agent definition containing instructions and configuration.</param>
    /// <param name="contextItems">Optional context items to populate the runtime context with.</param>
    /// <param name="additionalTools">Optional additional tools to include in the agent (e.g., frontend tools).</param>
    /// <param name="additionalProperties">Optional additional properties to set in the runtime context
    ///  (e.g., RunId, ThreadId for telemetry/logging).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An <see cref="AIAgent"/> ready for use with MAF's RunAsync/RunStreamingAsync methods.</returns>
    /// <remarks>
    /// <para>
    /// The returned agent automatically manages runtime context per-execution:
    /// <list type="bullet">
    ///   <item>Creates a fresh runtime context scope for each <c>RunAsync</c> or <c>RunStreamingAsync</c> call</item>
    ///   <item>Populates the scope with context items and invokes context contributors</item>
    ///   <item>Automatically injects system message parts from contributors into the conversation</item>
    ///   <item>Disposes the scope after each execution completes</item>
    ///   <item>Provides complete isolation between concurrent or sequential requests</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Example:</strong>
    /// </para>
    /// <code>
    /// // Create agent once
    /// var agent = await _agentFactory.CreateAgentAsync(
    ///     agentDefinition,
    ///     contextItems: new[] { new AIRequestContextItem { Description = "userId", Value = userId } },
    ///     additionalTools: frontendTools,
    ///     additionalProperties: new Dictionary&lt;string, object?&gt; { ["RunId"] = runId },
    ///     cancellationToken);
    ///
    /// // Reuse for multiple requests - each gets fresh scope and context
    /// var result1 = await agent.RunAsync(messages1, session: null, options: null, ct);
    /// var result2 = await agent.RunAsync(messages2, session: null, options: null, ct);
    ///
    /// // No manual scope management needed - all automatic
    /// </code>
    /// </remarks>
    Task<AIAgent> CreateAgentAsync(
        AIAgent agent,
        IEnumerable<AIRequestContextItem>? contextItems = null,
        IEnumerable<AITool>? additionalTools = null,
        IReadOnlyDictionary<string, object?>? additionalProperties = null,
        CancellationToken cancellationToken = default);
}
