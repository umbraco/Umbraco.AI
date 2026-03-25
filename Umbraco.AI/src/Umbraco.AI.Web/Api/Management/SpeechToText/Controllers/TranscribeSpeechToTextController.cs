using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.SpeechToText;
using Umbraco.AI.Extensions;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.AI.Web.Api.Management.SpeechToText.Models;

#pragma warning disable MEAI001 // SpeechToTextOptions/SpeechToTextResponse are experimental in M.E.AI

namespace Umbraco.AI.Web.Api.Management.SpeechToText.Controllers;

/// <summary>
/// Controller to transcribe audio to text.
/// </summary>
[ApiVersion("1.0")]
public class TranscribeSpeechToTextController : SpeechToTextControllerBase
{
    private readonly IAISpeechToTextService _speechToTextService;
    private readonly IAIProfileService _profileService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranscribeSpeechToTextController"/> class.
    /// </summary>
    public TranscribeSpeechToTextController(
        IAISpeechToTextService speechToTextService,
        IAIProfileService profileService)
    {
        _speechToTextService = speechToTextService;
        _profileService = profileService;
    }

    /// <summary>
    /// Transcribe an audio file to text.
    /// </summary>
    /// <param name="audioFile">The audio file to transcribe.</param>
    /// <param name="profileIdOrAlias">Optional profile ID or alias to use for transcription.</param>
    /// <param name="language">Optional BCP-47 language hint (e.g., "en", "de").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transcribed text.</returns>
    [HttpPost("transcribe")]
    [MapToApiVersion("1.0")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(SpeechToTextResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TranscribeAudio(
        [FromForm] IFormFile audioFile,
        [FromQuery] string? profileIdOrAlias = null,
        [FromQuery] string? language = null,
        CancellationToken cancellationToken = default)
    {
        if (audioFile is null || audioFile.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid audio file",
                Detail = "An audio file must be provided.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            var options = new SpeechToTextOptions
            {
                SpeechLanguage = language
            };

            // Resolve profile ID from IdOrAlias
            var profileId = profileIdOrAlias != null
                ? await _profileService.TryGetProfileIdAsync(IdOrAlias.Parse(profileIdOrAlias, null), cancellationToken)
                : null;

            await using var audioStream = audioFile.OpenReadStream();

            var result = profileId.HasValue
                ? await _speechToTextService.TranscribeAsync(profileId.Value, audioStream, options, cancellationToken)
                : await _speechToTextService.TranscribeAsync(audioStream, options, cancellationToken);

            var response = new SpeechToTextResponseModel
            {
                Text = result.Text ?? string.Empty
            };

            return Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return ProfileNotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Transcription failed",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }
}
