using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Agent.Core.Agents;
using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Core.RuntimeContext;
using Umbraco.Ai.Core.Tools;
using Umbraco.Ai.Extensions;
using CoreConstants = Umbraco.Ai.Core.Constants;

namespace Umbraco.Ai.Agent.Core.Chat;

/// <summary>
/// Factory for creating MAF AIAgent instances from agent definitions.
/// </summary>
internal sealed class AiAgentFactory : IAiAgentFactory
{
    private readonly IAiRuntimeContextScopeProvider _runtimeContextScopeProvider;
    private readonly AiRuntimeContextContributorCollection _contextContributors;
    private readonly IAiProfileService _profileService;
    private readonly IAiChatClientFactory _chatClientFactory;
    private readonly AiToolCollection _toolCollection;
    private readonly IAiFunctionFactory _functionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiAgentFactory"/> class.
    /// </summary>
    public AiAgentFactory(
        IAiRuntimeContextScopeProvider runtimeContextScopeProvider,
        AiRuntimeContextContributorCollection contextContributors,
        IAiProfileService profileService,
        IAiChatClientFactory chatClientFactory,
        AiToolCollection toolCollection,
        IAiFunctionFactory functionFactory)
    {
        _runtimeContextScopeProvider = runtimeContextScopeProvider ?? throw new ArgumentNullException(nameof(runtimeContextScopeProvider));
        _contextContributors = contextContributors ?? throw new ArgumentNullException(nameof(contextContributors));
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _chatClientFactory = chatClientFactory ?? throw new ArgumentNullException(nameof(chatClientFactory));
        _toolCollection = toolCollection ?? throw new ArgumentNullException(nameof(toolCollection));
        _functionFactory = functionFactory ?? throw new ArgumentNullException(nameof(functionFactory));
    }

    /// <inheritdoc />
    public async Task<AIAgent> CreateAgentAsync(
        AiAgent agent,
        IEnumerable<AiRequestContextItem>? contextItems = null,
        IEnumerable<AITool>? additionalTools = null,
        IReadOnlyDictionary<string, object?>? additionalProperties = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);

        // Build tool list
        var tools = new List<AITool>();
        tools.AddRange(_toolCollection.ToSystemToolFunctions(_functionFactory));
        tools.AddRange(_toolCollection.ToUserToolFunctions(_functionFactory));

        var frontendTools = additionalTools?.ToList() ?? [];
        if (frontendTools.Count > 0)
        {
            tools.AddRange(frontendTools);
        }

        // Get profile - use default Chat profile if not specified
        AiProfile profile;
        if (agent.ProfileId.HasValue)
        {
            profile = await _profileService.GetProfileAsync(agent.ProfileId.Value, cancellationToken)
                ?? throw new InvalidOperationException($"Profile with ID '{agent.ProfileId}' not found.");
        }
        else
        {
            profile = await _profileService.GetDefaultProfileAsync(AiCapability.Chat, cancellationToken);
        }

        var chatClient = await _chatClientFactory.CreateClientAsync(profile, cancellationToken);

        // Create inner MAF agent (without runtime context metadata - that's set in ScopedAIAgent)
        var innerAgent = new ChatClientAgent(
            chatClient,
            instructions: agent.Instructions,
            name: agent.Name,
            description: agent.Description,
            tools: tools);

        // Wrap in scoped decorator (passes scope provider, contributors, and metadata)
        return new ScopedAIAgent(
            innerAgent,
            agent,
            contextItems ?? [],
            frontendTools,
            additionalProperties,
            _runtimeContextScopeProvider,
            _contextContributors);
    }
}
