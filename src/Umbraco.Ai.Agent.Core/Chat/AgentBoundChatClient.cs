using System.Text;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Agent.Core.Agents;
using Umbraco.Ai.Agent.Core.Context;
using Umbraco.Ai.Core.Chat;

namespace Umbraco.Ai.Agent.Core.Chat;

/// <summary>
/// A chat client decorator that binds an agent's configuration to all requests.
/// </summary>
/// <remarks>
/// <para>
/// This decorator performs two key functions:
/// </para>
/// <list type="number">
///   <item>Injects the <see cref="AgentContextResolver.AgentIdKey"/> into
///   <see cref="ChatOptions.AdditionalProperties"/> for context resolution.</item>
///   <item>Prepends the agent's <see cref="AiAgent.Instructions"/> as a system message.</item>
/// </list>
/// <para>
/// <strong>Alternative Approach:</strong> Instructions could be passed via
/// <c>ChatClientAgentOptions.Instructions</c> when creating the ChatClientAgent.
/// MAF will automatically prepend these. However, using this decorator provides:
/// </para>
/// <list type="number">
///   <item>Consistency with ProfileBoundChatClient pattern</item>
///   <item>Control over instruction placement and formatting</item>
///   <item>Ability to dynamically modify instructions per-request</item>
/// </list>
/// </remarks>
internal sealed class AgentBoundChatClient : BoundChatClientBase
{
    private readonly AiAgent _agent;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentBoundChatClient"/> class.
    /// </summary>
    /// <param name="innerClient">The inner chat client to delegate to.</param>
    /// <param name="agent">The agent definition containing instructions and configuration.</param>
    public AgentBoundChatClient(IChatClient innerClient, AiAgent agent)
        : base(innerClient)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
    }

    /// <inheritdoc />
    protected override ChatOptions? TransformOptions(ChatOptions? options)
    {
        options ??= new ChatOptions();
        options.AdditionalProperties ??= new AdditionalPropertiesDictionary();
        options.AdditionalProperties.TryAdd(AgentContextResolver.AgentIdKey, _agent.Id);
        return options;
    }

    /// <inheritdoc />
    protected override IEnumerable<ChatMessage> TransformMessages(IEnumerable<ChatMessage> chatMessages)
    {
        if (string.IsNullOrWhiteSpace(_agent.Instructions))
        {
            return chatMessages;
        }

        var messagesList = chatMessages.ToList();

        // Check if first message is already a system message
        if (messagesList.Count > 0 && messagesList[0].Role == ChatRole.System)
        {
            // Prepend agent instructions to existing system message
            var existingSystemMessage = messagesList[0];
            var combinedContent = new StringBuilder();
            combinedContent.AppendLine(_agent.Instructions);
            combinedContent.AppendLine();
            combinedContent.Append(existingSystemMessage.Text);

            messagesList[0] = new ChatMessage(ChatRole.System, combinedContent.ToString());
            return messagesList;
        }

        // Insert new system message at the beginning
        var systemMessage = new ChatMessage(ChatRole.System, _agent.Instructions);
        messagesList.Insert(0, systemMessage);
        return messagesList;
    }
}
