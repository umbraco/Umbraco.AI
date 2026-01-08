using Umbraco.Ai.Core.EditableModels;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Providers;

namespace Umbraco.Ai.Tests.Common.Fakes;

/// <summary>
/// Fake implementation of <see cref="IAiProvider"/> for use in tests.
/// </summary>
public class FakeAiProvider : IAiProvider
{
    private readonly Dictionary<Type, IAiCapability> _capabilities = new();

    public FakeAiProvider(string id = "fake-provider", string name = "Fake Provider")
    {
        Id = id;
        Name = name;
    }

    public string Id { get; }

    public string Name { get; }

    public Type? SettingsType { get; set; } = typeof(FakeProviderSettings);

    public AiEditableModelSchema? SettingsSchema { get; set; } = new AiEditableModelSchema(
        typeof(FakeProviderSettings),
        new List<AiEditableModelField>
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

    public FakeAiProvider WithCapability<TCapability>(TCapability capability) where TCapability : IAiCapability
    {
        _capabilities[typeof(TCapability)] = capability;
        return this;
    }

    public FakeAiProvider WithChatCapability(IAiChatCapability? capability = null)
    {
        _capabilities[typeof(IAiChatCapability)] = capability ?? new FakeChatCapability();
        return this;
    }

    public FakeAiProvider WithEmbeddingCapability(IAiEmbeddingCapability? capability = null)
    {
        _capabilities[typeof(IAiEmbeddingCapability)] = capability ?? new FakeEmbeddingCapability();
        return this;
    }

    public FakeAiProvider WithSettingsType<TSettings>() where TSettings : class
    {
        SettingsType = typeof(TSettings);
        return this;
    }

    public FakeAiProvider WithSettingsSchema(AiEditableModelSchema? schema)
    {
        SettingsSchema = schema;
        return this;
    }

    public AiEditableModelSchema? GetSettingsSchema() => SettingsSchema;

    public IReadOnlyCollection<IAiCapability> GetCapabilities() => _capabilities.Values.ToList();

    public bool TryGetCapability<TCapability>(out TCapability? capability) where TCapability : class, IAiCapability
    {
        if (_capabilities.TryGetValue(typeof(TCapability), out var cap) && cap is TCapability typed)
        {
            capability = typed;
            return true;
        }

        capability = default;
        return false;
    }

    public TCapability? GetCapability<TCapability>() where TCapability : class, IAiCapability
    {
        return _capabilities.TryGetValue(typeof(TCapability), out var cap) ? cap as TCapability : null;
    }

    public bool HasCapability<TCapability>() where TCapability : class, IAiCapability
    {
        return _capabilities.ContainsKey(typeof(TCapability));
    }
}
