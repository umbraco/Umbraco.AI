using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Extensions;
using CoreConstants = Umbraco.Ai.Core.Constants;

namespace Umbraco.AI.Agent.Core.Chat;

/// <summary>
/// Factory for creating MAF AIAgent instances from agent definitions.
/// </summary>
internal sealed class AIAgentFactory : IAIAgentFactory
{
    private readonly IAIRuntimeContextScopeProvider _runtimeContextScopeProvider;
    private readonly AIRuntimeContextContributorCollection _contextContributors;
    private readonly IAIProfileService _profileService;
    private readonly IAIChatClientFactory _chatClientFactory;
    private readonly AIToolCollection _toolCollection;
    private readonly IAIFunctionFactory _functionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIAgentFactory"/> class.
    /// </summary>
    public AIAgentFactory(
        IAIRuntimeContextScopeProvider runtimeContextScopeProvider,
        AIRuntimeContextContributorCollection contextContributors,
        IAIProfileService profileService,
        IAIChatClientFactory chatClientFactory,
        AIToolCollection toolCollection,
        IAIFunctionFactory functionFactory)
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
        AIAgent agent,
        IEnumerable<AIRequestContextItem>? contextItems = null,
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
        AIProfile profile;
        if (agent.ProfileId.HasValue)
        {
            profile = await _profileService.GetProfileAsync(agent.ProfileId.Value, cancellationToken)
                ?? throw new InvalidOperationException($"Profile with ID '{agent.ProfileId}' not found.");
        }
        else
        {
            profile = await _profileService.GetDefaultProfileAsync(AICapability.Chat, cancellationToken);
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
