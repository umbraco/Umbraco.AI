using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Scopes;
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
    private readonly AIToolScopeCollection _toolScopeCollection;
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
        AIToolScopeCollection toolScopeCollection,
        IAIFunctionFactory functionFactory)
    {
        _runtimeContextScopeProvider = runtimeContextScopeProvider ?? throw new ArgumentNullException(nameof(runtimeContextScopeProvider));
        _contextContributors = contextContributors ?? throw new ArgumentNullException(nameof(contextContributors));
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _chatClientFactory = chatClientFactory ?? throw new ArgumentNullException(nameof(chatClientFactory));
        _toolCollection = toolCollection ?? throw new ArgumentNullException(nameof(toolCollection));
        _toolScopeCollection = toolScopeCollection ?? throw new ArgumentNullException(nameof(toolScopeCollection));
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

        // STEP 1: Get allowed tool IDs (permission check - existing logic)
        var allowedToolIds = AIAgentToolHelper.GetAllowedToolIds(agent, _toolCollection);

        // STEP 2: Create runtime context and run contributors (NEW)
        AIRuntimeContext? runtimeContext = null;
        if (contextItems?.Any() == true)
        {
            runtimeContext = new AIRuntimeContext(contextItems);
            foreach (var contributor in _contextContributors)
            {
                contributor.Contribute(runtimeContext);
            }
        }

        // STEP 3: Filter backend tools by runtime context
        //         Only filters context-bound tools (those with ForEntityTypes declared)
        //         Cross-context tools (no ForEntityTypes) are always included
        var contextFilteredToolIds = AIToolContextFilter.FilterByContext(
            allowedToolIds,
            runtimeContext,
            _toolCollection,
            _toolScopeCollection);

        // STEP 4: Build tool list using context-filtered backend tools
        var tools = new List<AITool>();
        tools.AddRange(_toolCollection.ToAIFunctions(contextFilteredToolIds, _functionFactory));

        // STEP 5: Filter frontend tools by runtime context
        //         Frontend tools are context-bound if their scope declares ForEntityTypes
        if (additionalTools is not null)
        {
            var contextFilteredFrontendTools = FilterFrontendToolsByContext(
                additionalTools,
                runtimeContext,
                _toolScopeCollection);
            tools.AddRange(contextFilteredFrontendTools);
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

    /// <summary>
    /// Filters frontend tools based on runtime context.
    /// Only filters context-bound tools (those with scopes that declare ForEntityTypes).
    /// </summary>
    private static IEnumerable<AITool> FilterFrontendToolsByContext(
        IEnumerable<AITool> frontendTools,
        AIRuntimeContext? runtimeContext,
        AIToolScopeCollection scopeCollection)
    {
        // No runtime context = no filtering (return all)
        if (runtimeContext == null)
            return frontendTools;

        // Extract current entity type
        var currentEntityType = runtimeContext.GetValue<string>(CoreConstants.ContextKeys.EntityType);

        // No entity type context = no filtering (return all)
        if (string.IsNullOrEmpty(currentEntityType))
            return frontendTools;

        // Filter tools by entity type context
        return frontendTools.Where(tool =>
        {
            // Check if tool is AIFrontendToolFunction with scope information
            if (tool is not AIFrontendToolFunction frontendToolFunction)
                return true; // Unknown tool type = include (backwards compatible)

            // No scope = cross-context tool = include
            if (string.IsNullOrEmpty(frontendToolFunction.Scope))
                return true;

            // Get scope and check if it's context-bound
            var scope = scopeCollection.GetById(frontendToolFunction.Scope);
            if (scope == null)
                return true; // Unknown scope = include (backwards compatible)

            // Check if scope is context-bound (declares entity types)
            var relevantEntityTypes = scope.ForEntityTypes;
            if (relevantEntityTypes.Count == 0)
                return true; // No entity types declared = cross-context tool = include

            // Context-bound tool: check if current entity type matches
            return relevantEntityTypes.Contains(currentEntityType, StringComparer.OrdinalIgnoreCase);
        });
    }
}
