using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Tests.Common.Builders;

/// <summary>
/// Fluent builder for creating <see cref="AiProfile"/> instances in tests.
/// </summary>
public class AiProfileBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _alias = "test-profile";
    private string _name = "Test Profile";
    private AiCapability _capability = AiCapability.Chat;
    private AiModelRef _model = new("test-provider", "test-model");
    private Guid _connectionId = Guid.NewGuid();
    private float? _temperature;
    private int? _maxTokens;
    private string? _systemPromptTemplate;
    private IReadOnlyList<string> _tags = Array.Empty<string>();

    public AiProfileBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public AiProfileBuilder WithAlias(string alias)
    {
        _alias = alias;
        return this;
    }

    public AiProfileBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public AiProfileBuilder WithCapability(AiCapability capability)
    {
        _capability = capability;
        return this;
    }

    public AiProfileBuilder WithModel(string providerId, string modelId)
    {
        _model = new AiModelRef(providerId, modelId);
        return this;
    }

    public AiProfileBuilder WithModel(AiModelRef model)
    {
        _model = model;
        return this;
    }

    public AiProfileBuilder WithConnectionId(Guid connectionId)
    {
        _connectionId = connectionId;
        return this;
    }

    public AiProfileBuilder WithTemperature(float? temperature)
    {
        _temperature = temperature;
        return this;
    }

    public AiProfileBuilder WithMaxTokens(int? maxTokens)
    {
        _maxTokens = maxTokens;
        return this;
    }

    public AiProfileBuilder WithSystemPromptTemplate(string? systemPromptTemplate)
    {
        _systemPromptTemplate = systemPromptTemplate;
        return this;
    }

    public AiProfileBuilder WithTags(params string[] tags)
    {
        _tags = tags;
        return this;
    }

    public AiProfile Build()
    {
        return new AiProfile
        {
            Id = _id,
            Alias = _alias,
            Name = _name,
            Capability = _capability,
            Model = _model,
            ConnectionId = _connectionId,
            Temperature = _temperature,
            MaxTokens = _maxTokens,
            SystemPromptTemplate = _systemPromptTemplate,
            Tags = _tags
        };
    }

    public static implicit operator AiProfile(AiProfileBuilder builder) => builder.Build();
}
