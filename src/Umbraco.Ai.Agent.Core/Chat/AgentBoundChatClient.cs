using System.Text;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Agent.Core.Agents;
using Umbraco.Ai.Core.Chat;
using CoreConstants = Umbraco.Ai.Core.Constants;

namespace Umbraco.Ai.Agent.Core.Chat;

/// <summary>
/// A chat client decorator that binds an agent's configuration to all requests.
/// </summary>
internal sealed class AgentBoundChatClient : BoundChatClientBase
{
    private readonly AiAgent _agent;
    private readonly IEnumerable<KeyValuePair<string, object?>>? _additionalProperties;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentBoundChatClient"/> class.
    /// </summary>
    /// <param name="innerClient">The inner chat client to delegate to.</param>
    /// <param name="agent">The agent definition containing instructions and configuration.</param>
    /// <param name="additionalProperties">Additional properties to pass to the chat options</param>
    public AgentBoundChatClient(IChatClient innerClient, AiAgent agent, 
        IEnumerable<KeyValuePair<string, object?>>? additionalProperties = null)
        : base(innerClient)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _additionalProperties = additionalProperties;
    }

    /// <inheritdoc />
    protected override ChatOptions? TransformOptions(ChatOptions? options)
    {
        options ??= new ChatOptions();
        options.AdditionalProperties ??= new AdditionalPropertiesDictionary();
        options.AdditionalProperties.TryAdd(Constants.MetadataKeys.AgentId, _agent.Id);
        options.AdditionalProperties.TryAdd(Constants.MetadataKeys.AgentAlias, _agent.Alias);
        options.AdditionalProperties.TryAdd(CoreConstants.MetadataKeys.FeatureType, "agent");
        options.AdditionalProperties.TryAdd(CoreConstants.MetadataKeys.FeatureId, _agent.Id);
        options.AdditionalProperties.TryAdd(CoreConstants.MetadataKeys.FeatureAlias, _agent.Alias);
        
        if (_additionalProperties != null)
        {
            foreach (var kvp in _additionalProperties)
            {
                options.AdditionalProperties[kvp.Key] = kvp.Value;
            }
        }
        
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
