using Microsoft.Extensions.AI;

#pragma warning disable MEAI001 // ISpeechToTextClient is experimental in M.E.AI

namespace Umbraco.AI.Core.SpeechToText;

/// <summary>
/// Base class for speech-to-text client decorators that wrap cross-cutting concerns.
/// </summary>
/// <remarks>
/// Provides virtual methods to transform the audio stream and options before delegation.
/// Subclasses override <see cref="TransformOptions"/> to customize behavior.
/// </remarks>
public abstract class AIBoundSpeechToTextClientBase : ISpeechToTextClient
{
    /// <summary>
    /// Gets the inner speech-to-text client that this decorator delegates to.
    /// </summary>
    protected ISpeechToTextClient InnerClient { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AIBoundSpeechToTextClientBase"/> class.
    /// </summary>
    /// <param name="innerClient">The inner speech-to-text client to delegate to.</param>
    protected AIBoundSpeechToTextClientBase(ISpeechToTextClient innerClient)
    {
        InnerClient = innerClient ?? throw new ArgumentNullException(nameof(innerClient));
    }

    /// <inheritdoc />
    public virtual Task<SpeechToTextResponse> GetTextAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return InnerClient.GetTextAsync(
            audioSpeechStream,
            TransformOptions(options),
            cancellationToken);
    }

    /// <inheritdoc />
    public virtual IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingTextAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return InnerClient.GetStreamingTextAsync(
            audioSpeechStream,
            TransformOptions(options),
            cancellationToken);
    }

    /// <inheritdoc />
    public virtual object? GetService(Type serviceType, object? key = null)
    {
        // Return self if this exact type is requested
        if (serviceType == GetType())
        {
            return this;
        }

        // Delegate to inner client for other services
        return InnerClient.GetService(serviceType, key);
    }

    /// <inheritdoc />
    public virtual void Dispose()
    {
        InnerClient.Dispose();
    }

    /// <summary>
    /// Transforms the speech-to-text options before passing to the inner client.
    /// </summary>
    /// <param name="options">The original speech-to-text options.</param>
    /// <returns>The transformed speech-to-text options.</returns>
    protected virtual SpeechToTextOptions? TransformOptions(SpeechToTextOptions? options)
        => options;
}
