using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Agent.Core.EmbeddedAgents;

/// <summary>
/// Fluent builder for configuring embedded agents — agents that run purely in code
/// without being managed through the backoffice UI.
/// </summary>
/// <remarks>
/// <para>
/// Embedded agents are ideal for CMS extensions that need agentic capabilities
/// without exposing the agent in the backoffice. They participate in the full
/// middleware pipeline (auditing, tracking, guardrails, telemetry) and can use
/// profiles and registered tools.
/// </para>
/// <para>
/// <strong>Standard agent example:</strong>
/// </para>
/// <code>
/// var response = await agentService.RunEmbeddedAgentAsync(agent => agent
///     .WithAlias("my-summarizer")
///     .WithInstructions("Summarize the provided content concisely.")
///     .WithToolScopes("content-read")
///     .WithProfile(profileId),
///     messages, cancellationToken);
/// </code>
/// <para>
/// <strong>Orchestrated agent example:</strong>
/// </para>
/// <code>
/// var agent = await agentService.CreateEmbeddedAgentAsync(a => a
///     .WithAlias("my-pipeline")
///     .WithWorkflow("sequential-pipeline", settings));
/// </code>
/// </remarks>
public sealed class AIEmbeddedAgentBuilder
{
    // Namespace GUID for deterministic ID generation (UUID v5)
    private static readonly Guid EmbeddedAgentNamespace = new("A7E3F4B1-2C8D-4E6F-9A1B-3D5E7F9A1B2C");

    private string? _alias;
    private string? _name;
    private string? _description;
    private Guid? _profileId;
    private string? _instructions;
    private bool _useAllTools;
    private readonly List<string> _toolIds = [];
    private readonly List<string> _toolScopeIds = [];
    private string? _workflowId;
    private JsonElement? _workflowSettings;
    private IEnumerable<AIRequestContextItem>? _contextItems;
    private IReadOnlyList<Guid> _guardrailIds = [];
    private IReadOnlyDictionary<string, object?>? _additionalProperties;

    /// <summary>
    /// Sets the alias for the embedded agent. Required for auditing and telemetry.
    /// </summary>
    /// <remarks>
    /// The alias is used to generate a deterministic ID, so the same alias always
    /// produces the same agent ID across invocations.
    /// </remarks>
    /// <param name="alias">A unique, URL-safe identifier for this embedded agent.</param>
    /// <returns>The builder for chaining.</returns>
    public AIEmbeddedAgentBuilder WithAlias(string alias)
    {
        _alias = alias;
        return this;
    }

    /// <summary>
    /// Sets the display name for the embedded agent.
    /// If not set, defaults to the alias.
    /// </summary>
    /// <param name="name">The display name.</param>
    /// <returns>The builder for chaining.</returns>
    public AIEmbeddedAgentBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    /// <summary>
    /// Sets the description for the embedded agent.
    /// </summary>
    /// <param name="description">The description of what this agent does.</param>
    /// <returns>The builder for chaining.</returns>
    public AIEmbeddedAgentBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    /// <summary>
    /// Sets the profile to use for AI model configuration.
    /// If not set, the default chat profile is used.
    /// </summary>
    /// <param name="profileId">The profile ID.</param>
    /// <returns>The builder for chaining.</returns>
    public AIEmbeddedAgentBuilder WithProfile(Guid profileId)
    {
        _profileId = profileId;
        return this;
    }

    /// <summary>
    /// Sets the system instructions for a standard agent.
    /// Mutually exclusive with <see cref="WithWorkflow"/>.
    /// </summary>
    /// <param name="instructions">The instructions that define agent behavior.</param>
    /// <returns>The builder for chaining.</returns>
    public AIEmbeddedAgentBuilder WithInstructions(string instructions)
    {
        _instructions = instructions;
        return this;
    }

    /// <summary>
    /// Includes all registered tools for this agent.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public AIEmbeddedAgentBuilder WithAllTools()
    {
        _useAllTools = true;
        return this;
    }

    /// <summary>
    /// Includes specific tools by their IDs.
    /// </summary>
    /// <param name="toolIds">The tool IDs to include.</param>
    /// <returns>The builder for chaining.</returns>
    public AIEmbeddedAgentBuilder WithTools(params string[] toolIds)
    {
        _toolIds.AddRange(toolIds);
        return this;
    }

    /// <summary>
    /// Includes all tools belonging to the specified scopes.
    /// </summary>
    /// <param name="scopeIds">The tool scope IDs to include.</param>
    /// <returns>The builder for chaining.</returns>
    public AIEmbeddedAgentBuilder WithToolScopes(params string[] scopeIds)
    {
        _toolScopeIds.AddRange(scopeIds);
        return this;
    }

    /// <summary>
    /// Configures this as an orchestrated agent using a registered workflow.
    /// Mutually exclusive with <see cref="WithInstructions"/>.
    /// </summary>
    /// <param name="workflowId">The ID of the registered workflow.</param>
    /// <param name="settings">Optional workflow-specific settings.</param>
    /// <returns>The builder for chaining.</returns>
    public AIEmbeddedAgentBuilder WithWorkflow(string workflowId, JsonElement? settings = null)
    {
        _workflowId = workflowId;
        _workflowSettings = settings;
        return this;
    }

    /// <summary>
    /// Sets context items to populate the runtime context with.
    /// </summary>
    /// <param name="contextItems">The context items.</param>
    /// <returns>The builder for chaining.</returns>
    public AIEmbeddedAgentBuilder WithContextItems(IEnumerable<AIRequestContextItem> contextItems)
    {
        _contextItems = contextItems;
        return this;
    }

    /// <summary>
    /// Sets guardrail IDs for safety and compliance checks.
    /// </summary>
    /// <param name="guardrailIds">The guardrail IDs to apply.</param>
    /// <returns>The builder for chaining.</returns>
    public AIEmbeddedAgentBuilder WithGuardrails(params Guid[] guardrailIds)
    {
        _guardrailIds = guardrailIds;
        return this;
    }

    /// <summary>
    /// Sets additional properties to include in the runtime context.
    /// </summary>
    /// <param name="properties">The additional properties.</param>
    /// <returns>The builder for chaining.</returns>
    public AIEmbeddedAgentBuilder WithAdditionalProperties(IReadOnlyDictionary<string, object?> properties)
    {
        _additionalProperties = properties;
        return this;
    }

    /// <summary>
    /// Gets whether all tools should be included.
    /// </summary>
    internal bool UseAllTools => _useAllTools;

    /// <summary>
    /// Gets the context items configured on this builder.
    /// </summary>
    internal IEnumerable<AIRequestContextItem>? ContextItems => _contextItems;

    /// <summary>
    /// Gets the additional properties configured on this builder.
    /// </summary>
    internal IReadOnlyDictionary<string, object?>? AdditionalProperties => _additionalProperties;

    /// <summary>
    /// Builds a transient <see cref="AIAgent"/> entity from the builder configuration.
    /// The entity is not persisted and is used only for the embedded agent execution pipeline.
    /// </summary>
    /// <returns>A transient <see cref="AIAgent"/> entity.</returns>
    /// <exception cref="InvalidOperationException">Thrown when required fields are missing or configuration is invalid.</exception>
    internal AIAgent Build()
    {
        if (string.IsNullOrWhiteSpace(_alias))
        {
            throw new InvalidOperationException("Embedded agent alias is required. Call WithAlias() before building.");
        }

        if (_workflowId is not null && _instructions is not null)
        {
            throw new InvalidOperationException(
                "Cannot configure both instructions and a workflow. Use WithInstructions() for standard agents or WithWorkflow() for orchestrated agents, not both.");
        }

        var isOrchestrated = _workflowId is not null;

        var agent = new AIAgent
        {
            Alias = _alias,
            Name = _name ?? _alias,
            Description = _description,
            AgentType = isOrchestrated ? AIAgentType.Orchestrated : AIAgentType.Standard,
            ProfileId = _profileId,
            GuardrailIds = _guardrailIds,
            IsActive = true,
            SurfaceIds = [],
        };

        // Set deterministic ID from alias (internal setter accessible within assembly)
        agent.Id = CreateDeterministicGuid(_alias);

        if (isOrchestrated)
        {
            agent.Config = new AIOrchestratedAgentConfig
            {
                WorkflowId = _workflowId,
                Settings = _workflowSettings,
            };
        }
        else
        {
            agent.Config = new AIStandardAgentConfig
            {
                Instructions = _instructions,
                AllowedToolIds = _toolIds,
                AllowedToolScopeIds = _toolScopeIds,
            };
        }

        return agent;
    }

    /// <summary>
    /// Creates a deterministic GUID from an alias using UUID v5 (SHA-1 based).
    /// The same alias always produces the same GUID.
    /// </summary>
    private static Guid CreateDeterministicGuid(string alias)
    {
        byte[] namespaceBytes = EmbeddedAgentNamespace.ToByteArray();
        byte[] aliasBytes = Encoding.UTF8.GetBytes(alias);

        byte[] combined = new byte[namespaceBytes.Length + aliasBytes.Length];
        Buffer.BlockCopy(namespaceBytes, 0, combined, 0, namespaceBytes.Length);
        Buffer.BlockCopy(aliasBytes, 0, combined, namespaceBytes.Length, aliasBytes.Length);

        byte[] hash = SHA1.HashData(combined);

        // Set version to 5 (name-based SHA-1)
        hash[6] = (byte)((hash[6] & 0x0F) | 0x50);
        // Set variant to RFC 4122
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);

        byte[] guidBytes = new byte[16];
        Array.Copy(hash, 0, guidBytes, 0, 16);

        return new Guid(guidBytes);
    }
}
