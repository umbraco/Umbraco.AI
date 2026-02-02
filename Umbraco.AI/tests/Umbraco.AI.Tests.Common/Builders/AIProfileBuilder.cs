using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;

namespace Umbraco.AI.Tests.Common.Builders;

/// <summary>
/// Fluent builder for creating <see cref="AIProfile"/> instances in tests.
/// </summary>
public class AIProfileBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _alias = "test-profile";
    private string _name = "Test Profile";
    private AICapability _capability = AICapability.Chat;
    private AIModelRef _model = new("test-provider", "test-model");
    private Guid _connectionId = Guid.NewGuid();
    private IAIProfileSettings? _settings;
    private IReadOnlyList<string> _tags = Array.Empty<string>();

    public AIProfileBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public AIProfileBuilder WithAlias(string alias)
    {
        _alias = alias;
        return this;
    }

    public AIProfileBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public AIProfileBuilder WithCapability(AICapability capability)
    {
        _capability = capability;
        return this;
    }

    public AIProfileBuilder WithModel(string providerId, string modelId)
    {
        _model = new AIModelRef(providerId, modelId);
        return this;
    }

    public AIProfileBuilder WithModel(AIModelRef model)
    {
        _model = model;
        return this;
    }

    public AIProfileBuilder WithConnectionId(Guid connectionId)
    {
        _connectionId = connectionId;
        return this;
    }

    public AIProfileBuilder WithSettings(IAIProfileSettings? settings)
    {
        _settings = settings;
        return this;
    }

    public AIProfileBuilder WithChatSettings(float? temperature = null, int? maxTokens = null, string? systemPromptTemplate = null)
    {
        _capability = AICapability.Chat;
        _settings = new AIChatProfileSettings
        {
            Temperature = temperature,
            MaxTokens = maxTokens,
            SystemPromptTemplate = systemPromptTemplate
        };
        return this;
    }

    public AIProfileBuilder WithEmbeddingSettings()
    {
        _capability = AICapability.Embedding;
        _settings = new AIEmbeddingProfileSettings();
        return this;
    }

    public AIProfileBuilder WithTags(params string[] tags)
    {
        _tags = tags;
        return this;
    }

    public AIProfile Build()
    {
        return new AIProfile
        {
            Id = _id,
            Alias = _alias,
            Name = _name,
            Capability = _capability,
            Model = _model,
            ConnectionId = _connectionId,
            Settings = _settings,
            Tags = _tags
        };
    }

    public static implicit operator AIProfile(AIProfileBuilder builder) => builder.Build();
}
