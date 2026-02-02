using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Core.RuntimeContext;
using CoreConstants = Umbraco.AI.Core.Constants;
using UmbracoAIAgent = Umbraco.AI.Agent.Core.Agents.AIAgent;
using MsAIAgent = Microsoft.Agents.AI.AIAgent;

namespace Umbraco.AI.Agent.Core.Chat;

/// <summary>
/// An AIAgent decorator that creates a runtime context scope per-execution
/// and automatically injects system message parts from context contributors.
/// </summary>
/// <remarks>
/// <para>
/// Each <c>Run</c> or <c>RunStreamingAsync</c> call creates a fresh scope, populates it via
/// contributors, injects system message parts, executes the inner agent, and disposes the scope.
/// This provides complete isolation between requests and automatic scope management.
/// </para>
/// <para>
/// <strong>Use this for scenarios requiring standalone agent instances</strong>, such as:
/// <list type="bullet">
///   <item>DevUI integration - expose agents to external consumers</item>
///   <item>Agent reusability - same instance handles multiple requests</item>
///   <item>Automatic context management - no manual scope handling required</item>
/// </list>
/// </para>
/// <para>
/// <strong>Usage Example:</strong>
/// </para>
/// <code>
/// // Create scoped agent
/// var agent = await _agentFactory.CreateScopedAgentAsync(
///     agentDefinition,
///     contextItems,
///     additionalTools,
///     cancellationToken);
///
/// // Use multiple times - each execution gets fresh scope
/// var result1 = await agent.RunAsync(messages1, null, ct);
/// var result2 = await agent.RunAsync(messages2, null, ct);
///
/// // Scopes are automatically disposed after each execution - no manual cleanup needed
/// </code>
/// </remarks>
internal sealed class ScopedAIAgent : MsAIAgent
{
    private readonly MsAIAgent _innerAgent;
    private readonly UmbracoAIAgent _definition;
    private readonly IReadOnlyList<AIRequestContextItem> _contextItems;
    private readonly IReadOnlyList<AITool> _frontendTools;
    private readonly IReadOnlyDictionary<string, object?>? _additionalProperties;
    private readonly IAIRuntimeContextScopeProvider _scopeProvider;
    private readonly AIRuntimeContextContributorCollection _contributors;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScopedAIAgent"/> class.
    /// </summary>
    /// <param name="innerAgent">The inner AIAgent to delegate execution to.</param>
    /// <param name="definition">The Umbraco agent definition this instance was created from.</param>
    /// <param name="contextItems">Context items to populate the scope with on each execution.</param>
    /// <param name="frontendTools">Frontend tools to track in the runtime context.</param>
    /// <param name="additionalProperties">Additional properties to set in the runtime context.</param>
    /// <param name="scopeProvider">Provider for creating runtime context scopes.</param>
    /// <param name="contributors">Collection of context contributors to populate the scope.</param>
    internal ScopedAIAgent(
        MsAIAgent innerAgent,
        UmbracoAIAgent definition,
        IEnumerable<AIRequestContextItem> contextItems,
        IEnumerable<AITool> frontendTools,
        IReadOnlyDictionary<string, object?>? additionalProperties,
        IAIRuntimeContextScopeProvider scopeProvider,
        AIRuntimeContextContributorCollection contributors)
    {
        _innerAgent = innerAgent ?? throw new ArgumentNullException(nameof(innerAgent));
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        _contextItems = contextItems?.ToList() ?? [];
        _frontendTools = frontendTools?.ToList() ?? [];
        _additionalProperties = additionalProperties;
        _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
        _contributors = contributors ?? throw new ArgumentNullException(nameof(contributors));
    }

    /// <summary>
    /// Gets the Umbraco agent definition this instance was created from.
    /// </summary>
    public UmbracoAIAgent Definition => _definition;

    /// <summary>
    /// Gets the human-readable name of the agent.
    /// </summary>
    public override string? Name => _definition.Name;

    /// <summary>
    /// Gets the description of the agent's purpose and capabilities.
    /// </summary>
    public override string? Description => _definition.Description;

    /// <summary>
    /// Implements the core run logic with scope management and system message injection.
    /// </summary>
    protected override async Task<AgentResponse> RunCoreAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Create scope for THIS execution
        using var scope = _scopeProvider.CreateScope(_contextItems);

        // Populate scope via contributors and set metadata
        PopulateScopeContext(scope.Context);

        // Inject system message parts from populated context
        var enhancedMessages = InjectSystemMessageParts(scope.Context, messages);

        // Execute inner agent (which calls its RunCoreAsync internally)
        return await _innerAgent.RunAsync(enhancedMessages, session, options, cancellationToken);

        // Scope automatically disposed by 'using' statement
    }

    /// <summary>
    /// Implements the core streaming run logic with scope management and system message injection.
    /// </summary>
    protected override async IAsyncEnumerable<AgentResponseUpdate> RunCoreStreamingAsync(
        IEnumerable<ChatMessage> messages,
        AgentSession? session = null,
        AgentRunOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Create scope for THIS execution
        using var scope = _scopeProvider.CreateScope(_contextItems);

        // Populate scope via contributors and set metadata
        PopulateScopeContext(scope.Context);

        // Inject system message parts from populated context
        var enhancedMessages = InjectSystemMessageParts(scope.Context, messages);

        // Execute inner agent (streaming)
        await foreach (var update in _innerAgent.RunStreamingAsync(enhancedMessages, session, options, cancellationToken))
        {
            yield return update;
        }

        // Scope automatically disposed by 'using' statement
    }

    /// <summary>
    /// Creates a new session for this agent (delegates to inner agent).
    /// </summary>
    public override ValueTask<AgentSession> GetNewSessionAsync(CancellationToken cancellationToken = default)
    {
        // Delegate to inner agent
        return _innerAgent.GetNewSessionAsync(cancellationToken);
    }

    /// <summary>
    /// Deserializes a session from JSON (delegates to inner agent).
    /// </summary>
    public override ValueTask<AgentSession> DeserializeSessionAsync(
        JsonElement serializedSession,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default)
    {
        // Delegate to inner agent
        return _innerAgent.DeserializeSessionAsync(serializedSession, jsonSerializerOptions, cancellationToken);
    }

    /// <summary>
    /// Populates the runtime context scope with contributors and sets agent metadata.
    /// </summary>
    /// <param name="context">The runtime context to populate.</param>
    private void PopulateScopeContext(AIRuntimeContext context)
    {
        // Populate scope via contributors
        _contributors.Populate(context);

        // Set agent metadata in context (moved from factory)
        context.SetValue(Constants.ContextKeys.AgentId, _definition.Id);
        context.SetValue(Constants.ContextKeys.AgentAlias, _definition.Alias);
        context.SetValue(CoreConstants.ContextKeys.FeatureType, "agent");
        context.SetValue(CoreConstants.ContextKeys.FeatureId, _definition.Id);
        context.SetValue(CoreConstants.ContextKeys.FeatureAlias, _definition.Alias);
        context.SetValue(CoreConstants.ContextKeys.FeatureVersion, _definition.Version);

        // Set additional properties (RunId, ThreadId, etc.)
        if (_additionalProperties != null)
        {
            foreach (var property in _additionalProperties)
            {
                context.SetValue(property.Key, property.Value);
            }
        }

        // Set frontend tool names for tool reordering middleware
        if (_frontendTools.Count > 0)
        {
            var frontendToolNames = _frontendTools.Select(t => t.Name).ToArray();
            context.SetValue(Constants.ContextKeys.FrontendToolNames, frontendToolNames);
        }
    }

    /// <summary>
    /// Injects system message parts from context into the message list.
    /// </summary>
    /// <param name="context">The runtime context containing system message parts.</param>
    /// <param name="messages">The original message list.</param>
    /// <returns>A new message list with system messages injected.</returns>
    private static IEnumerable<ChatMessage> InjectSystemMessageParts(
        AIRuntimeContext context,
        IEnumerable<ChatMessage> messages)
    {
        var messagesList = messages.ToList();

        // Check if context has system message parts
        if (context.SystemMessageParts.Count == 0)
        {
            return messagesList; // No injection needed
        }

        // Build system message from parts (matching streaming service pattern)
        var systemPrompt = string.Join("\n\n", context.SystemMessageParts);

        // Check if there's already a system message to combine with
        var existingSystemIndex = messagesList.FindIndex(m => m.Role == ChatRole.System);

        if (existingSystemIndex >= 0)
        {
            // Prepend to existing system message
            var existingContent = messagesList[existingSystemIndex].Text ?? string.Empty;
            var combinedContent = string.IsNullOrEmpty(existingContent)
                ? systemPrompt
                : $"{systemPrompt}\n\n{existingContent}";
            messagesList[existingSystemIndex] = new ChatMessage(ChatRole.System, combinedContent);
        }
        else
        {
            // Insert new system message at the beginning
            messagesList.Insert(0, new ChatMessage(ChatRole.System, systemPrompt));
        }

        return messagesList;
    }

}
