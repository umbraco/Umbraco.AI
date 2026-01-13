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
        options.AdditionalProperties.TryAdd(Constants.MetadataKeys.AgentId, _agent.Id);
        options.AdditionalProperties.TryAdd(Constants.MetadataKeys.AgentAlias, _agent.Alias);
        options.AdditionalProperties.TryAdd(CoreConstants.MetadataKeys.FeatureType, "agent");
        options.AdditionalProperties.TryAdd(CoreConstants.MetadataKeys.FeatureId, _agent.Id);
        options.AdditionalProperties.TryAdd(CoreConstants.MetadataKeys.FeatureAlias, _agent.Alias);
        return options;
    }
}
