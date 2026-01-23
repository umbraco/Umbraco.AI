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
    private readonly IAiRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAiProfileService _profileService;
    private readonly IAiChatClientFactory _chatClientFactory;
    private readonly AiToolCollection _toolCollection;
    private readonly IAiFunctionFactory _functionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiAgentFactory"/> class.
    /// </summary>
    public AiAgentFactory(
        IAiRuntimeContextAccessor runtimeContextAccessor,
        IAiProfileService profileService,
        IAiChatClientFactory chatClientFactory,
        AiToolCollection toolCollection,
        IAiFunctionFactory functionFactory)
    {
        _runtimeContextAccessor = runtimeContextAccessor ?? throw new ArgumentNullException(nameof(runtimeContextAccessor));
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _chatClientFactory = chatClientFactory ?? throw new ArgumentNullException(nameof(chatClientFactory));
        _toolCollection = toolCollection ?? throw new ArgumentNullException(nameof(toolCollection));
        _functionFactory = functionFactory ?? throw new ArgumentNullException(nameof(functionFactory));
    }

    /// <inheritdoc />
    public async Task<AIAgent> CreateAgentAsync(
        AiAgent agent,
        IEnumerable<AITool>? additionalTools = null,
        IEnumerable<KeyValuePair<string, object?>>? additionalProperties = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);

        // Build tool list
        var tools = new List<AITool>();
        tools.AddRange(_toolCollection.ToSystemToolFunctions(_functionFactory));
        tools.AddRange(_toolCollection.ToUserToolFunctions(_functionFactory));

        // Collect frontend tool names and add them to additional properties
        // AiToolReorderingChatMiddleware reads these to reorder tool calls
        var frontendToolNames = additionalTools?.Select(t => t.Name).ToList() ?? [];
        if (additionalTools != null)
        {
            tools.AddRange(additionalTools);
        }

        // Get profile - use default Chat profile if not specified
        // The factory applies all middleware including AiToolReorderingChatMiddleware
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

        // Set agent metadata in runtime context for auditing and telemetry
        if (_runtimeContextAccessor.Context is not null)
        {
            _runtimeContextAccessor.Context.SetValue(Constants.ContextKeys.AgentId, agent.Id);
            _runtimeContextAccessor.Context.SetValue(Constants.ContextKeys.AgentAlias, agent.Alias);
            
            _runtimeContextAccessor.Context.SetValue(CoreConstants.ContextKeys.FeatureType, "agent");
            _runtimeContextAccessor.Context.SetValue(CoreConstants.ContextKeys.FeatureId, agent.Id);
            _runtimeContextAccessor.Context.SetValue(CoreConstants.ContextKeys.FeatureAlias, agent.Alias);
            _runtimeContextAccessor.Context.SetValue(CoreConstants.ContextKeys.FeatureVersion, agent.Version);

            if (additionalProperties != null)
            {
                foreach (var allAdditionalProperty in additionalProperties)
                {
                    _runtimeContextAccessor.Context.SetValue(allAdditionalProperty.Key, allAdditionalProperty.Value);
                }
            }

            if (frontendToolNames.Count > 0)
            {
                _runtimeContextAccessor.Context.SetValue(
                    Constants.ContextKeys.FrontendToolNames,
                    frontendToolNames);
            }
        }

        // Create MAF ChatClientAgent using the extension method
        return chatClient.CreateAIAgent(
            name: agent.Name,
            description: agent.Description,
            tools: tools);
    }
}
