using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.AuditLog;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Observability;
using Umbraco.AI.Core.RuntimeContext;

#pragma warning disable MEAI001 // ISpeechToTextClient is experimental in M.E.AI

namespace Umbraco.AI.Core.SpeechToText;

/// <summary>
/// Speech-to-text client that records usage analytics and audit entries around a transcription, by
/// delegating to the shared <see cref="IAIOperationTracker"/>. Replaces the former separate
/// tracking/usage-recording/auditing speech-to-text client trio with a single tracker-backed client.
/// </summary>
internal sealed class AITrackingSpeechToTextClient : AIBoundSpeechToTextClientBase
{
    private readonly IAIOperationTracker _tracker;
    private readonly IAIRuntimeContextAccessor _contextAccessor;

    public AITrackingSpeechToTextClient(ISpeechToTextClient innerClient, IAIOperationTracker tracker, IAIRuntimeContextAccessor contextAccessor)
        : base(innerClient)
    {
        _tracker = tracker;
        _contextAccessor = contextAccessor;
    }

    /// <inheritdoc />
    public override async Task<SpeechToTextResponse> GetTextAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var descriptor = BuildDescriptor(options);

        var tracked = await _tracker.TrackAsync(
            descriptor,
            async token =>
            {
                var response = await base.GetTextAsync(audioSpeechStream, options, token);
                return new AITrackedOperationResult<SpeechToTextResponse>
                {
                    Result = response,
                    Usage = null,
                    AuditResponse = new AIAuditResponse { Data = response.Text },
                };
            },
            cancellationToken);

        return tracked.Result;
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingTextAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var descriptor = BuildDescriptor(options);

        var scope = await _tracker.BeginAsync(descriptor, cancellationToken);
        var textParts = new List<string>();
        Exception? captured = null;

        // yield cannot sit inside try/catch, so drive the enumerator manually (matches prior behavior).
        await using var enumerator = base.GetStreamingTextAsync(audioSpeechStream, options, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);
        try
        {
            while (true)
            {
                SpeechToTextResponseUpdate current;
                try
                {
                    if (!await enumerator.MoveNextAsync())
                    {
                        break;
                    }

                    current = enumerator.Current;
                }
                catch (Exception ex)
                {
                    captured = ex;
                    break;
                }

                if (current.Text is not null)
                {
                    textParts.Add(current.Text);
                }

                yield return current;
            }

            if (captured is not null)
            {
                await scope.FailAsync(captured);
                throw captured;
            }

            var concatenatedText = string.Concat(textParts);
            await scope.CompleteAsync(null, new AIAuditResponse { Data = concatenatedText });
        }
        finally
        {
            scope.Dispose();
        }
    }

    private AIOperationDescriptor BuildDescriptor(SpeechToTextOptions? options) => new()
    {
        Capability = AICapability.SpeechToText,
        PromptData = BuildPromptData(options),
        Metadata = AIAuditMetadata.ExtractFromRuntimeContext(_contextAccessor.Context),
        RecordUsageWhenEmpty = true,
    };

    /// <summary>
    /// Builds a descriptive prompt data object for audit logging.
    /// Since STT doesn't have text prompts, we capture the options metadata.
    /// </summary>
    private static object? BuildPromptData(SpeechToTextOptions? options)
    {
        if (options is null)
        {
            return "speech-to-text transcription";
        }

        return new
        {
            Type = "speech-to-text",
            options.ModelId,
            options.SpeechLanguage,
        };
    }
}
