using System.Reflection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Umbraco.AI.Automate.Actions;
using Umbraco.AI.Core.Media;
using Umbraco.AI.Core.SpeechToText;
using Umbraco.Automate.Core.Actions;
using Umbraco.Automate.Core.Settings;
using Xunit;

#pragma warning disable MEAI001 // SpeechToTextOptions / SpeechToTextResponse are experimental in M.E.AI

namespace Umbraco.AI.Automate.Tests.Unit.Actions;

public class TranscribeAudioActionTests
{
    private readonly Mock<IAISpeechToTextService> _speechToTextServiceMock = new();
    private readonly Mock<IAIUmbracoMediaResolver> _mediaResolverMock = new();
    private readonly Mock<ILogger<TranscribeAudioAction>> _loggerMock = new();
    private readonly ActionInfrastructure _infrastructure;

    public TranscribeAudioActionTests()
    {
        _infrastructure = new ActionInfrastructure(new Mock<IEditableModelResolver>().Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyPath_ReturnsValidationError()
    {
        // Arrange
        var action = CreateAction();
        var context = CreateContext(new TranscribeAudioSettings { AudioPath = string.Empty });

        // Act
        var result = await action.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnresolvableMedia_ReturnsValidationError()
    {
        // Arrange
        _mediaResolverMock
            .Setup(r => r.ResolveAsync(It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIMediaContent?)null);

        var action = CreateAction();
        var context = CreateContext(new TranscribeAudioSettings { AudioPath = "missing.mp3" });

        // Act
        var result = await action.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonAudioMedia_ReturnsValidationError()
    {
        // Arrange — resolver returns an image, not audio
        _mediaResolverMock
            .Setup(r => r.ResolveAsync(It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIMediaContent { Data = [0x89, 0x50, 0x4E, 0x47], MediaType = "image/png" });

        var action = CreateAction();
        var context = CreateContext(new TranscribeAudioSettings { AudioPath = "photo.png" });

        // Act
        var result = await action.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Validation);

        // Transcription must NOT be attempted for non-audio input
        _speechToTextServiceMock.Verify(
            s => s.TranscribeAsync(
                It.IsAny<Action<AISpeechToTextBuilder>>(),
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithAudioMedia_ReturnsTranscriptText()
    {
        // Arrange
        _mediaResolverMock
            .Setup(r => r.ResolveAsync(It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIMediaContent { Data = new byte[] { 0, 1, 2, 3 }, MediaType = "audio/mpeg" });

        _speechToTextServiceMock
            .Setup(s => s.TranscribeAsync(
                It.IsAny<Action<AISpeechToTextBuilder>>(),
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SpeechToTextResponse("Hello world."));

        var action = CreateAction();
        var context = CreateContext(new TranscribeAudioSettings { AudioPath = "voice-note.mp3" });

        // Act
        var result = await action.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ActionResultStatus.Success);
        result.OutputData.ShouldNotBeNull();

        var textValue = result.OutputData!
            .GetType()
            .GetProperty("text")!
            .GetValue(result.OutputData);
        textValue.ShouldBe("Hello world.");
    }

    [Fact]
    public async Task ExecuteAsync_WithProfileId_PassesProfileIdToBuilder()
    {
        // Arrange
        var profileId = Guid.NewGuid();

        _mediaResolverMock
            .Setup(r => r.ResolveAsync(It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIMediaContent { Data = new byte[] { 0, 1 }, MediaType = "audio/wav" });

        Action<AISpeechToTextBuilder>? capturedConfigure = null;
        _speechToTextServiceMock
            .Setup(s => s.TranscribeAsync(
                It.IsAny<Action<AISpeechToTextBuilder>>(),
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .Callback<Action<AISpeechToTextBuilder>, Stream, CancellationToken>(
                (cfg, _, _) => capturedConfigure = cfg)
            .ReturnsAsync(new SpeechToTextResponse("ok"));

        var action = CreateAction();
        var context = CreateContext(new TranscribeAudioSettings
        {
            AudioPath = "clip.wav",
            ProfileId = profileId,
        });

        // Act
        await action.ExecuteAsync(context, CancellationToken.None);

        // Assert — invoke the captured configure delegate and inspect the builder state
        capturedConfigure.ShouldNotBeNull();
        var builder = new AISpeechToTextBuilder();
        capturedConfigure(builder);

        GetPrivateField<Guid?>(builder, "_profileId").ShouldBe(profileId);
    }

    [Fact]
    public async Task ExecuteAsync_WithLanguage_PassesLanguageOptionToBuilder()
    {
        // Arrange
        _mediaResolverMock
            .Setup(r => r.ResolveAsync(It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIMediaContent { Data = new byte[] { 0, 1 }, MediaType = "audio/wav" });

        Action<AISpeechToTextBuilder>? capturedConfigure = null;
        _speechToTextServiceMock
            .Setup(s => s.TranscribeAsync(
                It.IsAny<Action<AISpeechToTextBuilder>>(),
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .Callback<Action<AISpeechToTextBuilder>, Stream, CancellationToken>(
                (cfg, _, _) => capturedConfigure = cfg)
            .ReturnsAsync(new SpeechToTextResponse("ok"));

        var action = CreateAction();
        var context = CreateContext(new TranscribeAudioSettings
        {
            AudioPath = "clip.wav",
            Language = "da-DK",
        });

        // Act
        await action.ExecuteAsync(context, CancellationToken.None);

        // Assert
        capturedConfigure.ShouldNotBeNull();
        var builder = new AISpeechToTextBuilder();
        capturedConfigure(builder);

        var options = GetPrivateField<SpeechToTextOptions?>(builder, "_speechToTextOptions");
        options.ShouldNotBeNull();
        options.SpeechLanguage.ShouldBe("da-DK");
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_ReturnsCancelledError()
    {
        // Arrange
        _mediaResolverMock
            .Setup(r => r.ResolveAsync(It.IsAny<object?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIMediaContent { Data = new byte[] { 0, 1 }, MediaType = "audio/mpeg" });

        _speechToTextServiceMock
            .Setup(s => s.TranscribeAsync(
                It.IsAny<Action<AISpeechToTextBuilder>>(),
                It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var action = CreateAction();
        var context = CreateContext(new TranscribeAudioSettings { AudioPath = "voice-note.mp3" });

        // Act
        var result = await action.ExecuteAsync(context, CancellationToken.None);

        // Assert
        result.Status.ShouldBe(ActionResultStatus.Failed);
        result.ErrorCategory.ShouldBe(StepRunErrorCategory.Cancelled);
    }

    private TranscribeAudioAction CreateAction()
        => new(_infrastructure, _speechToTextServiceMock.Object, _mediaResolverMock.Object, _loggerMock.Object);

    private static ActionContext CreateContext(TranscribeAudioSettings settings)
        => new()
        {
            AutomationId = Guid.NewGuid(),
            RunId = Guid.NewGuid(),
            StepId = Guid.NewGuid(),
            ActionAlias = UmbracoAIAutomateConstants.ActionTypes.TranscribeAudio,
            Settings = settings,
        };

    private static T? GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType()
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        field.ShouldNotBeNull($"Expected private field '{fieldName}' on {instance.GetType().Name}.");
        return (T?)field.GetValue(instance);
    }
}
