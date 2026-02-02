using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Providers;

namespace Umbraco.Ai.Tests.Common.Fakes;

/// <summary>
/// Fake implementation of <see cref="IAiChatCapability"/> for use in tests.
/// </summary>
public class FakeChatCapability : IAiChatCapability
{
    private readonly IChatClient _chatClient;
    private readonly IReadOnlyList<AiModelDescriptor> _models;

    public FakeChatCapability(IChatClient? chatClient = null, IReadOnlyList<AiModelDescriptor>? models = null)
    {
        _chatClient = chatClient ?? new FakeChatClient();
        _models = models ?? new List<AiModelDescriptor>
        {
            new(new AiModelRef("fake-provider", "fake-model-1"), "Fake Model 1"),
            new(new AiModelRef("fake-provider", "fake-model-2"), "Fake Model 2")
        };
    }

    public AiCapability Kind => AiCapability.Chat;

    public IChatClient CreateClient(object? settings = null, string? modelId = null)
    {
        return _chatClient;
    }

    public Task<IReadOnlyList<AiModelDescriptor>> GetModelsAsync(object? settings = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_models);
    }
}
