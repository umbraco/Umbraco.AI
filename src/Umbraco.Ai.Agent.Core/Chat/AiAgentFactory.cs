using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Agent.Core.Agents;
using Umbraco.Ai.Core.Chat;
using Umbraco.Ai.Core.Tools;
using Umbraco.Ai.Extensions;

namespace Umbraco.Ai.Agent.Core.Chat;

/// <summary>
/// Factory for creating MAF AIAgent instances from agent definitions.
/// </summary>
internal sealed class AiAgentFactory : IAiAgentFactory
{
    private readonly IAiChatService _chatService;
    private readonly AiToolCollection _toolCollection;
    private readonly IAiFunctionFactory _functionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiAgentFactory"/> class.
    /// </summary>
    /// <param name="chatService">The chat service for creating chat clients.</param>
    /// <param name="toolCollection">The collection of registered tools.</param>
    /// <param name="functionFactory">The factory for creating AI functions from tools.</param>
    public AiAgentFactory(
        IAiChatService chatService,
        AiToolCollection toolCollection,
        IAiFunctionFactory functionFactory)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _toolCollection = toolCollection ?? throw new ArgumentNullException(nameof(toolCollection));
        _functionFactory = functionFactory ?? throw new ArgumentNullException(nameof(functionFactory));
    }

    /// <inheritdoc />
    public async Task<AIAgent> CreateAgentAsync(
        AiAgent agent,
        IEnumerable<AITool>? additionalTools = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);

        // Get base chat client from profile (already has ProfileBoundChatClient wrapping)
        var chatClient = await _chatService.GetChatClientAsync(agent.ProfileId, cancellationToken);

        // Wrap with AgentBoundChatClient for agent-specific injection
        var agentBoundClient = new AgentBoundChatClient(chatClient, agent);

        // Build tool list - all tools included by default
        var tools = new List<AITool>();
        tools.AddRange(_toolCollection.ToSystemToolFunctions(_functionFactory));
        tools.AddRange(_toolCollection.ToUserToolFunctions(_functionFactory));

        // Add additional (likely frontend) tools if provided (for copilot UI)
        if (additionalTools != null)
        {
            tools.AddRange(additionalTools);
        }

        // Create MAF ChatClientAgent using the extension method
        return agentBoundClient.CreateAIAgent(
            name: agent.Name,
            description: agent.Description,
            instructions: agent.Instructions,
            tools: tools);
    }
}
