using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.Media;
using Umbraco.AI.Core.SpeechToText;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Security;
using Umbraco.Cms.Core;

#pragma warning disable MEAI001 // SpeechToTextOptions / ISpeechToTextClient are experimental in M.E.AI

namespace Umbraco.AI.Automate.Actions;

/// <summary>
/// An Automate action that resolves an audio file from an Umbraco media reference
/// (GUID, media picker value, or path) and transcribes it using an AI speech-to-text
/// profile. Returns the transcribed text.
/// </summary>
[Action(UmbracoAIAutomateConstants.ActionTypes.TranscribeAudio, "Transcribe Audio",
    Description = "Transcribes an audio file from an Umbraco media reference using an AI speech-to-text profile.",
    Group = "AI",
    Icon = "icon-mic",
    RequiredSections = [Constants.Applications.Media])]
public sealed class TranscribeAudioAction : ActionBase<TranscribeAudioSettings, object>
{
    private readonly IAISpeechToTextService _speechToTextService;
    private readonly IAIUmbracoMediaResolver _mediaResolver;
    private readonly IAutomationActionAuthorizer _authorizer;
    private readonly ILogger<TranscribeAudioAction> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranscribeAudioAction"/> class.
    /// </summary>
    public TranscribeAudioAction(
        ActionInfrastructure infrastructure,
        IAISpeechToTextService speechToTextService,
        IAIUmbracoMediaResolver mediaResolver,
        IAutomationActionAuthorizer authorizer,
        ILogger<TranscribeAudioAction> logger)
        : base(infrastructure)
    {
        _speechToTextService = speechToTextService;
        _mediaResolver = mediaResolver;
        _authorizer = authorizer;
        _logger = logger;
    }

    /// <inheritdoc />
    public override async Task<ActionResult> ExecuteAsync(ActionContext context, CancellationToken cancellationToken)
    {
        var settings = context.GetSettings<TranscribeAudioSettings>();

        if (string.IsNullOrWhiteSpace(settings.AudioPath))
        {
            return ActionResult.Failed(
                new ArgumentException("Audio source is required."),
                StepRunErrorCategory.Validation);
        }

        _logger.LogInformation(
            "Automation {AutomationId} / Run {RunId}: Transcribing audio from {AudioPath}",
            context.AutomationId, context.RunId, settings.AudioPath);

        try
        {
            AIMediaContent? media = await _mediaResolver.ResolveAsync(settings.AudioPath, cancellationToken: cancellationToken);
            if (media is null)
            {
                return ActionResult.Failed(
                    new InvalidOperationException($"Could not resolve audio file from '{settings.AudioPath}'."),
                    StepRunErrorCategory.Validation);
            }

            if (!media.MediaType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
            {
                return ActionResult.Failed(
                    new InvalidOperationException(
                        $"Resolved file is not an audio file (media type: {media.MediaType})."),
                    StepRunErrorCategory.Validation);
            }

            // Node-level authorisation: when the audio source was an Umbraco media reference,
            // the service account must have access to that node. Raw file paths bypass this
            // because they're not CMS nodes — the operator has explicitly configured them.
            if (media.MediaKey is { } mediaKey
                && await _authorizer.AuthorizeMediaOrFailAsync(mediaKey, cancellationToken) is { } failure)
            {
                return failure;
            }

            await using var audioStream = new MemoryStream(media.Data);

            SpeechToTextResponse response = await _speechToTextService.TranscribeAsync(
                b =>
                {
                    b.WithAlias("automate-transcription");

                    if (settings.ProfileId.HasValue && settings.ProfileId.Value != Guid.Empty)
                    {
                        b.WithProfile(settings.ProfileId.Value);
                    }

                    if (!string.IsNullOrWhiteSpace(settings.Language))
                    {
                        b.WithSpeechToTextOptions(new SpeechToTextOptions { SpeechLanguage = settings.Language });
                    }
                },
                audioStream,
                cancellationToken);

            var text = response.Text ?? string.Empty;

            _logger.LogDebug(
                "Automation {AutomationId} / Run {RunId}: Transcription completed, text length {TextLength}",
                context.AutomationId, context.RunId, text.Length);

            return Success(new { text });
        }
        catch (InvalidOperationException ex)
        {
            return ActionResult.Failed(ex, StepRunErrorCategory.Validation);
        }
        catch (OperationCanceledException ex)
        {
            return ActionResult.Failed(ex, StepRunErrorCategory.Cancelled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Automation {AutomationId} / Run {RunId}: Audio transcription failed for {AudioPath}",
                context.AutomationId, context.RunId, settings.AudioPath);
            return ActionResult.Failed(ex, StepRunErrorCategory.Unknown);
        }
    }
}
