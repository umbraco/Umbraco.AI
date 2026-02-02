using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Tests.Common.Fakes;

/// <summary>
/// Fake implementation of <see cref="IAIChatCapability"/> for use in tests.
/// </summary>
public class FakeChatCapability : IAIChatCapability
{
    private readonly IChatClient _chatClient;
    private readonly IReadOnlyList<AIModelDescriptor> _models;

    public FakeChatCapability(IChatClient? chatClient = null, IReadOnlyList<AIModelDescriptor>? models = null)
    {
        _chatClient = chatClient ?? new FakeChatClient();
        _models = models ?? new List<AIModelDescriptor>
        {
            new(new AIModelRef("fake-provider", "fake-model-1"), "Fake Model 1"),
            new(new AIModelRef("fake-provider", "fake-model-2"), "Fake Model 2")
        };
    }

    public AICapability Kind => AICapability.Chat;

    public IChatClient CreateClient(object? settings = null, string? modelId = null)
    {
        return _chatClient;
    }

    public Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(object? settings = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_models);
    }
}
