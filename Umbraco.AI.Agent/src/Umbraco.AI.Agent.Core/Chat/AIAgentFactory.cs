using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Extensions;
using CoreConstants = Umbraco.AI.Core.Constants;
using UmbracoAIAgent = Umbraco.AI.Agent.Core.Agents.AIAgent;
using MsAIAgent = Microsoft.Agents.AI.AIAgent;

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
    public async Task<MsAIAgent> CreateAgentAsync(
        UmbracoAIAgent agent,
        IEnumerable<AIRequestContextItem>? contextItems = null,
        IEnumerable<AITool>? additionalTools = null,
        IReadOnlyDictionary<string, object?>? additionalProperties = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);

        // Compute enabled tool IDs for this agent
        var enabledTools = new List<string>();

        // 1. Always include system tools
        var systemToolIds = _toolCollection
            .Where(t => t is IAISystemTool)
            .Select(t => t.Id);
        enabledTools.AddRange(systemToolIds);

        // 2. Add explicitly enabled tool IDs
        if (agent.AllowedToolIds.Count > 0)
        {
            enabledTools.AddRange(agent.AllowedToolIds);
        }

        // 3. Add tools from enabled scopes
        if (agent.AllowedToolScopeIds.Count > 0)
        {
            foreach (var scope in agent.AllowedToolScopeIds)
            {
                var scopeToolIds = _toolCollection.GetByScope(scope)
                    .Where(t => t is not IAISystemTool) // Don't duplicate system tools
                    .Select(t => t.Id);
                enabledTools.AddRange(scopeToolIds);
            }
        }

        // 4. Deduplicate
        var enabledToolIds = enabledTools
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // Build tool list using only enabled tools
        var tools = new List<AITool>();
        tools.AddRange(_toolCollection.ToAIFunctions(enabledToolIds, _functionFactory));

        // Frontend tools - already filtered by service layer, just add them
        if (additionalTools is not null)
        {
            tools.AddRange(additionalTools);
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
            additionalTools,
            additionalProperties,
            _runtimeContextScopeProvider,
            _contextContributors);
    }
}
