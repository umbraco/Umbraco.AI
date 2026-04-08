using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.SpeechToText;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="TranscribeSpeechToTextController"/> class.
    /// </summary>
    public TranscribeSpeechToTextController(IAISpeechToTextService speechToTextService)
    {
        _speechToTextService = speechToTextService;
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
        IFormFile audioFile,
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
            // Save to a temp file so M.E.AI's OpenAISpeechToTextClient picks up
            // the correct extension from FileStream.Name (it defaults to "audio.mp3"
            // for non-FileStream inputs, causing format mismatch).
            var extension = Path.GetExtension(audioFile.FileName);
            if (string.IsNullOrEmpty(extension))
            {
                extension = MimeTypeToExtension(audioFile.ContentType);
            }

            var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{extension}");
            try
            {
                await using (var fileStream = new FileStream(tempPath, FileMode.Create))
                {
                    await audioFile.CopyToAsync(fileStream, cancellationToken);
                }

                await using var audioStream = new FileStream(tempPath, FileMode.Open, FileAccess.Read);

                var result = await _speechToTextService.TranscribeAsync(
                    b =>
                    {
                        b.WithAlias("backoffice-transcription");

                        if (profileIdOrAlias is not null)
                        {
                            var idOrAlias = IdOrAlias.Parse(profileIdOrAlias, null);
                            if (idOrAlias.IsId)
                            {
                                b.WithProfile(idOrAlias.Id!.Value);
                            }
                            else
                            {
                                b.WithProfile(idOrAlias.Alias!);
                            }
                        }

                        if (language is not null)
                        {
                            b.WithSpeechToTextOptions(new SpeechToTextOptions { SpeechLanguage = language });
                        }
                    },
                    audioStream, cancellationToken);

                var response = new SpeechToTextResponseModel
                {
                    Text = result.Text ?? string.Empty
                };

                return Ok(response);
            }
            finally
            {
                if (System.IO.File.Exists(tempPath))
                {
                    System.IO.File.Delete(tempPath);
                }
            }
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

    private static string MimeTypeToExtension(string? contentType) => contentType?.ToLowerInvariant() switch
    {
        "audio/webm" => ".webm",
        "audio/ogg" => ".ogg",
        "audio/wav" or "audio/wave" => ".wav",
        "audio/mp3" or "audio/mpeg" => ".mp3",
        "audio/mp4" => ".mp4",
        "audio/flac" => ".flac",
        "audio/x-m4a" or "audio/m4a" => ".m4a",
        _ => ".webm"
    };
}
