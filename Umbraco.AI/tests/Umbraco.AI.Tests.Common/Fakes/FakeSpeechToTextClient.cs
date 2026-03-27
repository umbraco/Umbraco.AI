#pragma warning disable MEAI001 // ISpeechToTextClient is experimental in M.E.AI

using Microsoft.Extensions.AI;

namespace Umbraco.AI.Tests.Common.Fakes;

/// <summary>
/// Fake implementation of <see cref="ISpeechToTextClient"/> for use in tests.
/// </summary>
public class FakeSpeechToTextClient : ISpeechToTextClient
{
    private readonly string _defaultText;

    public FakeSpeechToTextClient(string defaultText = "This is a fake transcription.")
    {
        _defaultText = defaultText;
    }

    /// <summary>
    /// Gets the list of options that were passed to transcription calls.
    /// </summary>
    public List<SpeechToTextOptions?> ReceivedOptions { get; } = [];

    public SpeechToTextClientMetadata Metadata { get; } = new("FakeSpeechToTextClient", new Uri("https://fake.test"), "fake-stt-model");

    public Task<SpeechToTextResponse> GetTextAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ReceivedOptions.Add(options);
        return Task.FromResult(new SpeechToTextResponse(_defaultText));
    }

    public IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingTextAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Streaming transcription is not implemented in FakeSpeechToTextClient.");
    }

    public object? GetService(Type serviceType, object? key = null)
    {
        if (serviceType == typeof(ISpeechToTextClient) || serviceType == typeof(FakeSpeechToTextClient))
        {
            return this;
        }

        return null;
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}
