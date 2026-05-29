using System.Runtime.InteropServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.SpeechToText;

#pragma warning disable MEAI001 // SpeechToTextResponse is experimental in M.E.AI

namespace Umbraco.AI.Core.FileProcessing;

/// <summary>
/// Transcribes uploaded audio files to text using the default speech-to-text profile,
/// so that audio attachments can be understood by chat models.
/// </summary>
/// <remarks>
/// This handler only activates when a default speech-to-text profile is configured. When no
/// default is configured the audio is left untouched and passed through to the model, which
/// allows natively multimodal models to handle it themselves.
/// </remarks>
internal sealed class AudioTranscriptionFileProcessingHandler : IAIFileProcessingHandler
{
    private const string AudioMimeTypePrefix = "audio/";

    private readonly IAIProfileService _profileService;
    private readonly IAISpeechToTextService _speechToTextService;
    private readonly ILogger<AudioTranscriptionFileProcessingHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioTranscriptionFileProcessingHandler"/> class.
    /// </summary>
    public AudioTranscriptionFileProcessingHandler(
        IAIProfileService profileService,
        IAISpeechToTextService speechToTextService,
        ILogger<AudioTranscriptionFileProcessingHandler> logger)
    {
        _profileService = profileService;
        _speechToTextService = speechToTextService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> CanHandleAsync(string mimeType, CancellationToken cancellationToken = default)
    {
        if (!mimeType.StartsWith(AudioMimeTypePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Only claim audio files when a default speech-to-text profile is configured;
        // otherwise leave the audio untouched so a multimodal model can handle it natively.
        return await _profileService.HasDefaultProfileAsync(AICapability.SpeechToText, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AIFileProcessingResult> ProcessAsync(
        ReadOnlyMemory<byte> data,
        string mimeType,
        string? filename,
        CancellationToken cancellationToken = default)
    {
        await using var stream = MemoryMarshal.TryGetArray(data, out var segment)
            ? new MemoryStream(segment.Array!, segment.Offset, segment.Count, writable: false)
            : new MemoryStream(data.ToArray());

        var response = await _speechToTextService.TranscribeAsync(
            b => b.WithAlias("file-processing-transcription"),
            stream,
            cancellationToken);

        var text = response.Text ?? string.Empty;

        _logger.LogDebug(
            "Transcribed audio file \"{Filename}\" ({MimeType}), text length {TextLength}",
            filename, mimeType, text.Length);

        return new AIFileProcessingResult(text, WasTruncated: false);
    }
}
