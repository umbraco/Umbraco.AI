#pragma warning disable MEAI001 // ISpeechToTextClient is experimental in M.E.AI

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.SpeechToText;
using Umbraco.AI.Web.Api.Management.SpeechToText.Controllers;

namespace Umbraco.AI.Tests.Unit.Api.Management.SpeechToText;

public class TranscribeSpeechToTextControllerTests
{
    private readonly Mock<IAISpeechToTextService> _speechToTextServiceMock = new();
    private readonly Mock<IAIProfileService> _profileServiceMock = new();
    private readonly TranscribeSpeechToTextController _controller;

    public TranscribeSpeechToTextControllerTests()
    {
        _controller = new TranscribeSpeechToTextController(
            _speechToTextServiceMock.Object,
            _profileServiceMock.Object);
    }

    private static IFormFile CreateAudioFile()
    {
        var content = new byte[] { 1, 2, 3 };
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "audioFile", "test.mp3")
        {
            Headers = new HeaderDictionary(),
            ContentType = "audio/mp3"
        };
    }

    [Fact]
    public async Task TranscribeAudio_WithUnknownProfileAlias_Returns404AndNeverTranscribes()
    {
        // Arrange — use alias-based IdOrAlias so TryGetProfileIdAsync does a DB lookup
        _profileServiceMock
            .Setup(x => x.GetProfileByAliasAsync("non-existent-profile", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIProfile?)null);

        // Act
        var result = await _controller.TranscribeAudio(CreateAudioFile(), profileIdOrAlias: "non-existent-profile");

        // Assert
        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        var problemDetails = notFoundResult.Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Title.ShouldBe("Profile not found");
        _speechToTextServiceMock.Verify(
            x => x.TranscribeAsync(It.IsAny<Action<AISpeechToTextBuilder>>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TranscribeAudio_WithNonExistingProfileId_Returns404AndNeverTranscribes()
    {
        var profileId = Guid.NewGuid();
        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIProfile?)null);

        var result = await _controller.TranscribeAudio(CreateAudioFile(), profileIdOrAlias: profileId.ToString());

        var notFoundResult = result.ShouldBeOfType<NotFoundObjectResult>();
        notFoundResult.Value.ShouldBeOfType<ProblemDetails>().Title.ShouldBe("Profile not found");
        _speechToTextServiceMock.Verify(
            x => x.TranscribeAsync(It.IsAny<Action<AISpeechToTextBuilder>>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TranscribeAudio_WhenServiceReportsSomethingNotFound_Returns400WithTheReason()
    {
        _speechToTextServiceMock
            .Setup(x => x.TranscribeAsync(It.IsAny<Action<AISpeechToTextBuilder>>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Default SpeechToText profile with alias 'x' not found."));

        var result = await _controller.TranscribeAudio(CreateAudioFile());

        var problemDetails = result.ShouldBeOfType<BadRequestObjectResult>().Value.ShouldBeOfType<ProblemDetails>();
        problemDetails.Type.ShouldBe("Error");
        problemDetails.Detail.ShouldBe("Default SpeechToText profile with alias 'x' not found.");
    }
}
