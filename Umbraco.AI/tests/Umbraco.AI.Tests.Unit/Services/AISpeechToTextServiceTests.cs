#pragma warning disable MEAI001 // ISpeechToTextClient is experimental in M.E.AI

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.SpeechToText;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.AI.Tests.Common.Fakes;

namespace Umbraco.AI.Tests.Unit.Services;

public class AISpeechToTextServiceTests
{
    private readonly Mock<IAISpeechToTextClientFactory> _clientFactoryMock;
    private readonly Mock<IAIProfileService> _profileServiceMock;
    private readonly Mock<IOptionsMonitor<AIOptions>> _optionsMock;
    private readonly IAISpeechToTextService _service;

    public AISpeechToTextServiceTests()
    {
        _clientFactoryMock = new Mock<IAISpeechToTextClientFactory>();
        _profileServiceMock = new Mock<IAIProfileService>();
        _optionsMock = new Mock<IOptionsMonitor<AIOptions>>();
        _optionsMock.Setup(x => x.CurrentValue).Returns(new AIOptions
        {
            DefaultSpeechToTextProfileAlias = "default-stt"
        });

        _service = new AISpeechToTextService(
            _clientFactoryMock.Object,
            _profileServiceMock.Object,
            _optionsMock.Object);
    }

    #region TranscribeAsync - Default profile

    [Fact]
    public async Task TranscribeAsync_WithDefaultProfile_UsesDefaultProfile()
    {
        // Arrange
        var audioStream = new MemoryStream([1, 2, 3]);
        var expectedText = "Hello world";

        var defaultProfile = new AIProfileBuilder()
            .WithAlias("default-stt")
            .WithCapability(AICapability.SpeechToText)
            .WithModel("openai", "gpt-4o-transcribe")
            .Build();

        var fakeClient = new FakeSpeechToTextClient(expectedText);

        _profileServiceMock
            .Setup(x => x.GetDefaultProfileAsync(AICapability.SpeechToText, It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultProfile);

        _clientFactoryMock
            .Setup(x => x.CreateClientAsync(defaultProfile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeClient);

        // Act
        var result = await _service.TranscribeAsync(audioStream);

        // Assert
        result.ShouldNotBeNull();
        result.Text.ShouldBe(expectedText);
        _profileServiceMock.Verify(
            x => x.GetDefaultProfileAsync(AICapability.SpeechToText, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region TranscribeAsync - By profile ID

    [Fact]
    public async Task TranscribeAsync_WithProfileId_UsesSpecifiedProfile()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var audioStream = new MemoryStream([1, 2, 3]);
        var expectedText = "Specific profile transcription";

        var profile = new AIProfileBuilder()
            .WithId(profileId)
            .WithCapability(AICapability.SpeechToText)
            .WithModel("openai", "whisper-1")
            .Build();

        var fakeClient = new FakeSpeechToTextClient(expectedText);

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _clientFactoryMock
            .Setup(x => x.CreateClientAsync(profile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeClient);

        // Act
        var result = await _service.TranscribeAsync(profileId, audioStream);

        // Assert
        result.ShouldNotBeNull();
        result.Text.ShouldBe(expectedText);
        _profileServiceMock.Verify(
            x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TranscribeAsync_WithNonExistentProfileId_ThrowsInvalidOperationException()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var audioStream = new MemoryStream([1, 2, 3]);

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AIProfile?)null);

        // Act
        var act = () => _service.TranscribeAsync(profileId, audioStream);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain($"AI profile with ID '{profileId}' not found");
    }

    [Fact]
    public async Task TranscribeAsync_WithChatProfile_ThrowsInvalidOperationException()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var audioStream = new MemoryStream([1, 2, 3]);

        var chatProfile = new AIProfileBuilder()
            .WithId(profileId)
            .WithCapability(AICapability.Chat)
            .WithName("Chat Profile")
            .Build();

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatProfile);

        // Act
        var act = () => _service.TranscribeAsync(profileId, audioStream);

        // Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        exception.Message.ShouldContain("does not support speech-to-text capability");
    }

    #endregion

    #region TranscribeAsync - Options merging

    [Fact]
    public async Task TranscribeAsync_WithCallerLanguage_MergesWithProfileDefaults()
    {
        // Arrange
        var audioStream = new MemoryStream([1, 2, 3]);
        var callerOptions = new SpeechToTextOptions { SpeechLanguage = "de" };

        var profile = new AIProfileBuilder()
            .WithCapability(AICapability.SpeechToText)
            .WithModel("openai", "gpt-4o-transcribe")
            .Build();

        var fakeClient = new FakeSpeechToTextClient();

        _profileServiceMock
            .Setup(x => x.GetDefaultProfileAsync(AICapability.SpeechToText, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _clientFactoryMock
            .Setup(x => x.CreateClientAsync(profile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeClient);

        // Act
        await _service.TranscribeAsync(audioStream, callerOptions);

        // Assert
        fakeClient.ReceivedOptions.Count.ShouldBe(1);
        fakeClient.ReceivedOptions[0].ShouldNotBeNull();
        fakeClient.ReceivedOptions[0]!.SpeechLanguage.ShouldBe("de");
    }

    [Fact]
    public async Task TranscribeAsync_WithNullOptions_UsesProfileModelId()
    {
        // Arrange
        var audioStream = new MemoryStream([1, 2, 3]);

        var profile = new AIProfileBuilder()
            .WithCapability(AICapability.SpeechToText)
            .WithModel("openai", "gpt-4o-transcribe")
            .Build();

        var fakeClient = new FakeSpeechToTextClient();

        _profileServiceMock
            .Setup(x => x.GetDefaultProfileAsync(AICapability.SpeechToText, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _clientFactoryMock
            .Setup(x => x.CreateClientAsync(profile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeClient);

        // Act
        await _service.TranscribeAsync(audioStream, null);

        // Assert
        fakeClient.ReceivedOptions.Count.ShouldBe(1);
        fakeClient.ReceivedOptions[0].ShouldNotBeNull();
        fakeClient.ReceivedOptions[0]!.ModelId.ShouldBe("gpt-4o-transcribe");
    }

    #endregion

    #region GetSpeechToTextClientAsync

    [Fact]
    public async Task GetSpeechToTextClientAsync_WithNullProfileId_UsesDefaultProfile()
    {
        // Arrange
        var defaultProfile = new AIProfileBuilder()
            .WithCapability(AICapability.SpeechToText)
            .WithModel("openai", "gpt-4o-transcribe")
            .Build();

        var fakeClient = new FakeSpeechToTextClient();

        _profileServiceMock
            .Setup(x => x.GetDefaultProfileAsync(AICapability.SpeechToText, It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultProfile);

        _clientFactoryMock
            .Setup(x => x.CreateClientAsync(defaultProfile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeClient);

        // Act
        var client = await _service.GetSpeechToTextClientAsync();

        // Assert
        client.ShouldBe(fakeClient);
        _profileServiceMock.Verify(
            x => x.GetDefaultProfileAsync(AICapability.SpeechToText, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetSpeechToTextClientAsync_WithProfileId_UsesSpecifiedProfile()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var profile = new AIProfileBuilder()
            .WithId(profileId)
            .WithCapability(AICapability.SpeechToText)
            .WithModel("openai", "whisper-1")
            .Build();

        var fakeClient = new FakeSpeechToTextClient();

        _profileServiceMock
            .Setup(x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _clientFactoryMock
            .Setup(x => x.CreateClientAsync(profile, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeClient);

        // Act
        var client = await _service.GetSpeechToTextClientAsync(profileId);

        // Assert
        client.ShouldBe(fakeClient);
        _profileServiceMock.Verify(
            x => x.GetProfileAsync(profileId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
