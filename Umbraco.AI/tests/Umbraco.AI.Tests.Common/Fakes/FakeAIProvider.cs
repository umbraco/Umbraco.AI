using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Tests.Common.Fakes;

/// <summary>
/// Fake implementation of <see cref="IAIProvider"/> for use in tests.
/// </summary>
public class FakeAIProvider : IAIProvider
{
    private readonly Dictionary<Type, IAICapability> _capabilities = new();

    public FakeAIProvider(string id = "fake-provider", string name = "Fake Provider")
    {
        Id = id;
        Name = name;
    }

    public string Id { get; }

    public string Name { get; }

    public Type? SettingsType { get; set; } = typeof(FakeProviderSettings);

    public AIEditableModelSchema? SettingsSchema { get; set; } = new AIEditableModelSchema(
        typeof(FakeProviderSettings),
        new List<AIEditableModelField>
        {
            new()
            {
                PropertyName = "ApiKey",
                Key = "api-key",
                Label = "API Key",
                Description = "Enter your API key"
            },
            new()
            {
                PropertyName = "BaseUrl",
                Key = "base-url",
                Label = "Base URL",
                Description = "The base URL for the API"
            }
        });

    public FakeAIProvider WithCapability<TCapability>(TCapability capability) where TCapability : IAICapability
    {
        _capabilities[typeof(TCapability)] = capability;
        return this;
    }

    public FakeAIProvider WithChatCapability(IAIChatCapability? capability = null)
    {
        _capabilities[typeof(IAIChatCapability)] = capability ?? new FakeChatCapability();
        return this;
    }

    public FakeAIProvider WithEmbeddingCapability(IAIEmbeddingCapability? capability = null)
    {
        _capabilities[typeof(IAIEmbeddingCapability)] = capability ?? new FakeEmbeddingCapability();
        return this;
    }

    public FakeAIProvider WithSettingsType<TSettings>() where TSettings : class
    {
        SettingsType = typeof(TSettings);
        return this;
    }

    public FakeAIProvider WithSettingsSchema(AIEditableModelSchema? schema)
    {
        SettingsSchema = schema;
        return this;
    }

    public AIEditableModelSchema? GetSettingsSchema() => SettingsSchema;

    public IReadOnlyCollection<IAICapability> GetCapabilities() => _capabilities.Values.ToList();

    public bool TryGetCapability<TCapability>(out TCapability? capability) where TCapability : class, IAICapability
    {
        if (_capabilities.TryGetValue(typeof(TCapability), out var cap) && cap is TCapability typed)
        {
            capability = typed;
            return true;
        }

        capability = default;
        return false;
    }

    public TCapability? GetCapability<TCapability>() where TCapability : class, IAICapability
    {
        return _capabilities.TryGetValue(typeof(TCapability), out var cap) ? cap as TCapability : null;
    }

    public bool HasCapability<TCapability>() where TCapability : class, IAICapability
    {
        return _capabilities.ContainsKey(typeof(TCapability));
    }
}
