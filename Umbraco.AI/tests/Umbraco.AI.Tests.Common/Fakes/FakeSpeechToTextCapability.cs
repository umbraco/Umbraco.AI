#pragma warning disable MEAI001 // ISpeechToTextClient is experimental in M.E.AI

using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Tests.Common.Fakes;

/// <summary>
/// Fake implementation of <see cref="IAISpeechToTextCapability"/> for use in tests.
/// </summary>
public class FakeSpeechToTextCapability : IAISpeechToTextCapability
{
    private readonly ISpeechToTextClient _client;
    private readonly IReadOnlyList<AIModelDescriptor> _models;

    public FakeSpeechToTextCapability(ISpeechToTextClient? client = null, IReadOnlyList<AIModelDescriptor>? models = null)
    {
        _client = client ?? new FakeSpeechToTextClient();
        _models = models ?? new List<AIModelDescriptor>
        {
            new(new AIModelRef("fake-provider", "whisper-1"), "Whisper 1"),
            new(new AIModelRef("fake-provider", "gpt-4o-transcribe"), "GPT 4o Transcribe"),
        };
    }

    public AICapability Kind => AICapability.SpeechToText;

    public Task<ISpeechToTextClient> CreateClientAsync(object? settings = null, string? modelId = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_client);
    }

    public Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(object? settings = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_models);
    }
}
