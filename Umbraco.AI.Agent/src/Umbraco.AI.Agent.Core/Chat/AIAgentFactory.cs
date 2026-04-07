using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.Workflows;
using Umbraco.AI.Core.Chat;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Tools;
using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.AI.Agent.Extensions;
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
    private readonly AIAgentWorkflowCollection _workflowCollection;

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
        IAIFunctionFactory functionFactory,
        AIAgentWorkflowCollection workflowCollection)
    {
        _runtimeContextScopeProvider = runtimeContextScopeProvider ?? throw new ArgumentNullException(nameof(runtimeContextScopeProvider));
        _contextContributors = contextContributors ?? throw new ArgumentNullException(nameof(contextContributors));
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _chatClientFactory = chatClientFactory ?? throw new ArgumentNullException(nameof(chatClientFactory));
        _toolCollection = toolCollection ?? throw new ArgumentNullException(nameof(toolCollection));
        _toolScopeCollection = toolScopeCollection ?? throw new ArgumentNullException(nameof(toolScopeCollection));
        _functionFactory = functionFactory ?? throw new ArgumentNullException(nameof(functionFactory));
        _workflowCollection = workflowCollection ?? throw new ArgumentNullException(nameof(workflowCollection));
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

        MsAIAgent innerAgent = agent.AgentType switch
        {
            AIAgentType.Standard => await CreateStandardAgentAsync(agent, contextItems, additionalTools, cancellationToken),
            AIAgentType.Orchestrated => await CreateOrchestratedAgentAsync(
                agent,
                agent.GetOrchestratedConfig()
                    ?? throw new InvalidOperationException(
                        $"Agent '{agent.Name}' (ID: {agent.Id}) has type '{AIAgentType.Orchestrated}' but no orchestrated config."),
                cancellationToken),
            _ => throw new InvalidOperationException(
                $"Unsupported agent type '{agent.AgentType}' for agent '{agent.Name}' (ID: {agent.Id})."),
        };

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

    private async Task<MsAIAgent> CreateStandardAgentAsync(
        UmbracoAIAgent agent,
        IEnumerable<AIRequestContextItem>? contextItems,
        IEnumerable<AITool>? additionalTools,
        CancellationToken cancellationToken)
    {
        // STEP 1: Get allowed tool IDs (permission check - existing logic)
        var allowedToolIds = AIAgentToolHelper.GetAllowedToolIds(agent, _toolCollection);

        // STEP 2: Create runtime context and run contributors
        //         This provides context to the LLM via system messages (section, entity, user)
        AIRuntimeContext? runtimeContext = null;
        if (contextItems?.Any() == true)
        {
            runtimeContext = new AIRuntimeContext(contextItems);
            foreach (var contributor in _contextContributors)
            {
                contributor.Contribute(runtimeContext);
            }
        }

        // STEP 3: Build tool list with ALL allowed backend tools (no context filtering)
        //         Backend tools are NOT filtered by context - they're cross-context
        //         The LLM uses:
        //         - Runtime context (section, entity) from system messages
        //         - Tool metadata (ForEntityTypes) from enriched descriptions
        //         - User's question
        //         to make informed decisions about which tools to use
        var tools = new List<AITool>();
        tools.AddRange(_toolCollection.ToAIFunctions(allowedToolIds, _functionFactory));

        // STEP 4: Filter frontend tools by runtime context
        //         Frontend tools ARE context-bound (operate on currently open entity)
        //         Only send tools relevant to the current entity type
        if (additionalTools is not null)
        {
            var contextFilteredFrontendTools = FilterFrontendToolsByContext(
                additionalTools,
                runtimeContext,
                _toolScopeCollection);
            tools.AddRange(contextFilteredFrontendTools);
        }

        // Get profile - use default Chat profile if not specified
        var chatClient = await CreateChatClientAsync(agent, cancellationToken);

        var config = agent.GetStandardConfig();

        // Build ChatOptions — always needed for instructions and tools,
        // plus output schema response format if configured
        var chatOptions = new ChatOptions
        {
            Instructions = config?.Instructions,
            Tools = tools,
        };

        if (config?.OutputSchema is JsonElement schema)
        {
            chatOptions.ResponseFormat = AIOutputSchema.FromJsonSchema(schema).ResponseFormat;
        }

        return new ChatClientAgent(chatClient, new ChatClientAgentOptions
        {
            Name = agent.Name,
            Description = agent.Description,
            ChatOptions = chatOptions
        });
    }

    private async Task<MsAIAgent> CreateOrchestratedAgentAsync(
        UmbracoAIAgent agent,
        AIOrchestratedAgentConfig config,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(config.WorkflowId))
        {
            throw new InvalidOperationException(
                $"Orchestrated agent '{agent.Name}' (ID: {agent.Id}) has no workflow configured.");
        }

        var workflow = _workflowCollection.GetById(config.WorkflowId)
            ?? throw new InvalidOperationException(
                $"Workflow '{config.WorkflowId}' not found for agent '{agent.Name}' (ID: {agent.Id}).");

        var mafWorkflow = await workflow.BuildWorkflowAsync(agent, config.Settings, cancellationToken);

        return mafWorkflow.AsAIAgent(config.WorkflowId);
    }

    private async Task<IChatClient> CreateChatClientAsync(
        UmbracoAIAgent agent,
        CancellationToken cancellationToken)
    {
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

        return await _chatClientFactory.CreateClientAsync(profile, cancellationToken);
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
