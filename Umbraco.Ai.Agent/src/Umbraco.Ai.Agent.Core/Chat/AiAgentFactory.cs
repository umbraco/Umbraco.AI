using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Agent.Core.Agents;
using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Core.Tools;
using Umbraco.Ai.Extensions;

namespace Umbraco.Ai.Agent.Core.Chat;

/// <summary>
/// Factory for creating MAF AIAgent instances from agent definitions.
/// </summary>
internal sealed class AiAgentFactory : IAiAgentFactory
{
    private readonly IAiProfileService _profileService;
    private readonly IAiChatClientFactory _chatClientFactory;
    private readonly AiToolCollection _toolCollection;
    private readonly IAiFunctionFactory _functionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiAgentFactory"/> class.
    /// </summary>
    public AiAgentFactory(
        IAiProfileService profileService,
        IAiChatClientFactory chatClientFactory,
        AiToolCollection toolCollection,
        IAiFunctionFactory functionFactory)
    {
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

        var allAdditionalProperties = new List<KeyValuePair<string, object?>>();
        if (additionalProperties != null)
        {
            allAdditionalProperties.AddRange(additionalProperties);
        }
        allAdditionalProperties.Add(new KeyValuePair<string, object?>(
            Constants.ChatOptionsKeys.FrontendToolNames,
            frontendToolNames));

        if (additionalTools != null)
        {
            tools.AddRange(additionalTools);
        }

        // Get profile and create chat client using standard factory
        // The factory applies all middleware including AiToolReorderingChatMiddleware
        var profile = await _profileService.GetProfileAsync(agent.ProfileId, cancellationToken)
            ?? throw new InvalidOperationException($"Profile with ID '{agent.ProfileId}' not found.");

        var chatClient = await _chatClientFactory.CreateClientAsync(profile, cancellationToken);

        // Wrap with AgentBoundChatClient for agent-specific injection
        // This adds frontend tool names to ChatOptions.AdditionalProperties for the reordering middleware
        var agentBoundClient = new AiAgentBoundChatClient(chatClient, agent, allAdditionalProperties);

        // Create MAF ChatClientAgent using the extension method
        return agentBoundClient.CreateAIAgent(
            name: agent.Name,
            description: agent.Description,
            tools: tools);
    }
}
