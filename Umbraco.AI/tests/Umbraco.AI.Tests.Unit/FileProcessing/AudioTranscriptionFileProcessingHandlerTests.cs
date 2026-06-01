using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Umbraco.AI.Core.FileProcessing;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.SpeechToText;

#pragma warning disable MEAI001 // SpeechToTextResponse is experimental in M.E.AI

namespace Umbraco.AI.Tests.Unit.FileProcessing;

public class AudioTranscriptionFileProcessingHandlerTests
{
    private readonly Mock<IAIProfileService> _profileService = new();
    private readonly Mock<IAISpeechToTextService> _speechToTextService = new();

    private AudioTranscriptionFileProcessingHandler CreateHandler()
        => new(_profileService.Object, _speechToTextService.Object, NullLogger<AudioTranscriptionFileProcessingHandler>.Instance);

    #region CanHandleAsync

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("image/png")]
    [InlineData("text/plain")]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    public async Task CanHandleAsync_WithNonAudioMimeType_ReturnsFalse(string mimeType)
    {
        var handler = CreateHandler();

        var result = await handler.CanHandleAsync(mimeType);

        result.ShouldBeFalse();
        // Should short-circuit before checking for a configured profile.
        _profileService.Verify(
            s => s.HasDefaultProfileAsync(It.IsAny<AICapability>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("audio/mpeg")]
    [InlineData("audio/wav")]
    [InlineData("AUDIO/MP4")]
    public async Task CanHandleAsync_WithAudioMimeType_AndDefaultProfileConfigured_ReturnsTrue(string mimeType)
    {
        _profileService
            .Setup(s => s.HasDefaultProfileAsync(AICapability.SpeechToText, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = CreateHandler();

        var result = await handler.CanHandleAsync(mimeType);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task CanHandleAsync_WithAudioMimeType_AndNoDefaultProfile_ReturnsFalse()
    {
        _profileService
            .Setup(s => s.HasDefaultProfileAsync(AICapability.SpeechToText, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var handler = CreateHandler();

        var result = await handler.CanHandleAsync("audio/mpeg");

        result.ShouldBeFalse();
    }

    #endregion

    #region ProcessAsync

    [Fact]
    public async Task ProcessAsync_TranscribesAudioStream_ReturnsTranscribedText()
    {
        const string transcription = "Hello from the audio file.";
        _speechToTextService
            .Setup(s => s.TranscribeAsync(
                It.IsAny<Action<AISpeechToTextBuilder>>(),
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SpeechToTextResponse(transcription));
        var handler = CreateHandler();

        var result = await handler.ProcessAsync(new byte[] { 1, 2, 3 }, "audio/mpeg", "recording.mp3");

        result.Content.ShouldBe(transcription);
        result.WasTruncated.ShouldBeFalse();
    }

    [Fact]
    public async Task ProcessAsync_WithNullTranscriptionText_ReturnsEmptyContent()
    {
        _speechToTextService
            .Setup(s => s.TranscribeAsync(
                It.IsAny<Action<AISpeechToTextBuilder>>(),
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SpeechToTextResponse((string?)null));
        var handler = CreateHandler();

        var result = await handler.ProcessAsync(new byte[] { 1, 2, 3 }, "audio/mpeg", "recording.mp3");

        result.Content.ShouldBe(string.Empty);
    }

    #endregion
}
