#pragma warning disable MEAI001 // SpeechToTextOptions is experimental in M.E.AI

using Microsoft.Extensions.AI;
using Shouldly;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.SpeechToText;
using Xunit;

namespace Umbraco.AI.Tests.Unit.SpeechToText;

public class AISpeechToTextBuilderTests
{
    [Fact]
    public void Validate_WithoutAlias_Throws()
    {
        // Arrange
        var builder = new AISpeechToTextBuilder();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Validate())
            .Message.ShouldContain("alias");
    }

    [Fact]
    public void Validate_WithAlias_DoesNotThrow()
    {
        // Arrange
        var builder = new AISpeechToTextBuilder();
        builder.WithAlias("test-stt");

        // Act & Assert
        Should.NotThrow(() => builder.Validate());
    }

    [Fact]
    public void Id_SameAlias_ProducesSameId()
    {
        // Arrange
        var builder1 = new AISpeechToTextBuilder();
        builder1.WithAlias("deterministic-test");

        var builder2 = new AISpeechToTextBuilder();
        builder2.WithAlias("deterministic-test");

        // Assert
        builder1.Id.ShouldBe(builder2.Id);
        builder1.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Id_DifferentAliases_ProduceDifferentIds()
    {
        // Arrange
        var builder1 = new AISpeechToTextBuilder();
        builder1.WithAlias("stt-alpha");

        var builder2 = new AISpeechToTextBuilder();
        builder2.WithAlias("stt-beta");

        // Assert
        builder1.Id.ShouldNotBe(builder2.Id);
    }

    [Fact]
    public void Id_DifferentNamespace_FromChatBuilder()
    {
        // The same alias should produce different IDs for chat vs speech-to-text
        // because they use different namespace GUIDs
        var sttBuilder = new AISpeechToTextBuilder();
        sttBuilder.WithAlias("same-alias");

        var chatBuilder = new Core.InlineChat.AIChatBuilder();
        chatBuilder.WithAlias("same-alias");

        // Assert
        sttBuilder.Id.ShouldNotBe(chatBuilder.Id);
    }

    [Fact]
    public void Name_DefaultsToAlias()
    {
        // Arrange
        var builder = new AISpeechToTextBuilder();
        builder.WithAlias("my-stt");

        // Assert
        builder.Name.ShouldBe("my-stt");
    }

    [Fact]
    public void Name_WhenExplicitlySet_UsesSetValue()
    {
        // Arrange
        var builder = new AISpeechToTextBuilder();
        builder.WithAlias("my-stt").WithName("My Transcription");

        // Assert
        builder.Name.ShouldBe("My Transcription");
    }

    [Fact]
    public void ProfileId_DefaultsToNull()
    {
        // Arrange
        var builder = new AISpeechToTextBuilder();

        // Assert
        builder.ProfileId.ShouldBeNull();
    }

    [Fact]
    public void WithProfile_ById_SetsProfileId()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var builder = new AISpeechToTextBuilder();
        builder.WithProfile(profileId);

        // Assert
        builder.ProfileId.ShouldBe(profileId);
        builder.ProfileAlias.ShouldBeNull();
    }

    [Fact]
    public void WithProfile_ByAlias_SetsProfileAlias()
    {
        // Arrange
        var builder = new AISpeechToTextBuilder();
        builder.WithProfile("whisper-profile");

        // Assert
        builder.ProfileAlias.ShouldBe("whisper-profile");
        builder.ProfileId.ShouldBeNull();
    }

    [Fact]
    public void WithProfile_ByAlias_ClearsProfileId()
    {
        // Arrange
        var builder = new AISpeechToTextBuilder();
        builder.WithProfile(Guid.NewGuid());

        // Act
        builder.WithProfile("whisper-profile");

        // Assert
        builder.ProfileId.ShouldBeNull();
        builder.ProfileAlias.ShouldBe("whisper-profile");
    }

    [Fact]
    public void WithSpeechToTextOptions_StoresOptions()
    {
        // Arrange
        var builder = new AISpeechToTextBuilder();
        var options = new SpeechToTextOptions { SpeechLanguage = "de" };

        // Act
        builder.WithSpeechToTextOptions(options);

        // Assert
        builder.SpeechToTextOptions.ShouldBe(options);
    }

    [Fact]
    public void SpeechToTextOptions_DefaultsToNull()
    {
        // Arrange
        var builder = new AISpeechToTextBuilder();

        // Assert
        builder.SpeechToTextOptions.ShouldBeNull();
    }

    [Fact]
    public void WithGuardrails_ById_StoresAdditionalIds()
    {
        var guardrailId = Guid.NewGuid();
        var builder = new AISpeechToTextBuilder();
        builder.WithGuardrails(guardrailId);

        builder.AdditionalGuardrailIds.ShouldContain(guardrailId);
        builder.AdditionalGuardrailAliases.ShouldBeNull();
    }

    [Fact]
    public void WithGuardrails_ByAlias_StoresAdditionalAliases()
    {
        var builder = new AISpeechToTextBuilder();
        builder.WithGuardrails("content-filter");

        builder.AdditionalGuardrailAliases.ShouldContain("content-filter");
        builder.AdditionalGuardrailIds.ShouldBeEmpty();
    }

    [Fact]
    public void SetGuardrails_ById_StoresReplaceIds()
    {
        var guardrailId = Guid.NewGuid();
        var builder = new AISpeechToTextBuilder();
        builder.SetGuardrails(guardrailId);

        builder.GuardrailIds.ShouldContain(guardrailId);
        builder.GuardrailAliases.ShouldBeNull();
    }

    [Fact]
    public void WithContextItems_StoresContextItems()
    {
        // Arrange
        var builder = new AISpeechToTextBuilder();
        var items = new[]
        {
            new AIRequestContextItem { Description = "test", Value = "value" }
        };

        // Act
        builder.WithContextItems(items);

        // Assert
        builder.ContextItems.ShouldBe(items);
    }

    [Fact]
    public void WithDescription_StoresDescription()
    {
        // Arrange
        var builder = new AISpeechToTextBuilder();

        // Act
        builder.WithDescription("A test description");

        // Assert
        builder.Description.ShouldBe("A test description");
    }

    [Fact]
    public void WithAdditionalProperties_StoresProperties()
    {
        // Arrange
        var builder = new AISpeechToTextBuilder();
        var properties = new Dictionary<string, object?> { { "key", "value" } };

        // Act
        builder.WithAdditionalProperties(properties);

        // Assert
        builder.AdditionalProperties.ShouldBe(properties);
    }

    [Fact]
    public void AsPassThrough_SetsFlag()
    {
        // Arrange
        var builder = new AISpeechToTextBuilder();

        // Act
        builder.AsPassThrough();

        // Assert
        builder.IsPassThrough.ShouldBeTrue();
    }

    [Fact]
    public void IsPassThrough_DefaultsToFalse()
    {
        // Arrange
        var builder = new AISpeechToTextBuilder();

        // Assert
        builder.IsPassThrough.ShouldBeFalse();
    }
}
