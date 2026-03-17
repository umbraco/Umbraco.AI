using Microsoft.Extensions.AI;
using Shouldly;
using Umbraco.AI.Core.InlineChat;
using Umbraco.AI.Core.RuntimeContext;
using Xunit;

namespace Umbraco.AI.Tests.Unit.InlineChat;

public class AIChatBuilderTests
{
    [Fact]
    public void Validate_WithoutAlias_Throws()
    {
        // Arrange
        var builder = new AIChatBuilder();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Validate())
            .Message.ShouldContain("alias");
    }

    [Fact]
    public void Validate_WithAlias_DoesNotThrow()
    {
        // Arrange
        var builder = new AIChatBuilder();
        builder.WithAlias("test-chat");

        // Act & Assert
        Should.NotThrow(() => builder.Validate());
    }

    [Fact]
    public void Id_SameAlias_ProducesSameId()
    {
        // Arrange
        var builder1 = new AIChatBuilder();
        builder1.WithAlias("deterministic-test");

        var builder2 = new AIChatBuilder();
        builder2.WithAlias("deterministic-test");

        // Assert
        builder1.Id.ShouldBe(builder2.Id);
        builder1.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Id_DifferentAliases_ProduceDifferentIds()
    {
        // Arrange
        var builder1 = new AIChatBuilder();
        builder1.WithAlias("chat-alpha");

        var builder2 = new AIChatBuilder();
        builder2.WithAlias("chat-beta");

        // Assert
        builder1.Id.ShouldNotBe(builder2.Id);
    }

    [Fact]
    public void Name_DefaultsToAlias()
    {
        // Arrange
        var builder = new AIChatBuilder();
        builder.WithAlias("my-chat");

        // Assert
        builder.Name.ShouldBe("my-chat");
    }

    [Fact]
    public void Name_WhenExplicitlySet_UsesSetValue()
    {
        // Arrange
        var builder = new AIChatBuilder();
        builder.WithAlias("my-chat").WithName("My Chat");

        // Assert
        builder.Name.ShouldBe("My Chat");
    }

    [Fact]
    public void ProfileId_DefaultsToNull()
    {
        // Arrange
        var builder = new AIChatBuilder();

        // Assert
        builder.ProfileId.ShouldBeNull();
    }

    [Fact]
    public void WithProfile_SetsProfileId()
    {
        // Arrange
        var profileId = Guid.NewGuid();
        var builder = new AIChatBuilder();
        builder.WithProfile(profileId);

        // Assert
        builder.ProfileId.ShouldBe(profileId);
    }

    [Fact]
    public void WithChatOptions_StoresOptions()
    {
        // Arrange
        var builder = new AIChatBuilder();
        var options = new ChatOptions { Temperature = 0.5f, MaxOutputTokens = 100 };

        // Act
        builder.WithChatOptions(options);

        // Assert
        builder.ChatOptions.ShouldBe(options);
    }

    [Fact]
    public void ChatOptions_DefaultsToNull()
    {
        // Arrange
        var builder = new AIChatBuilder();

        // Assert
        builder.ChatOptions.ShouldBeNull();
    }

    [Fact]
    public void WithGuardrails_SetsGuardrailIds()
    {
        // Arrange
        var guardrailId = Guid.NewGuid();
        var builder = new AIChatBuilder();
        builder.WithGuardrails(guardrailId);

        // Assert
        builder.GuardrailIds.ShouldContain(guardrailId);
    }

    [Fact]
    public void WithContextItems_StoresContextItems()
    {
        // Arrange
        var builder = new AIChatBuilder();
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
        var builder = new AIChatBuilder();

        // Act
        builder.WithDescription("A test description");

        // Assert
        builder.Description.ShouldBe("A test description");
    }

    [Fact]
    public void WithAdditionalProperties_StoresProperties()
    {
        // Arrange
        var builder = new AIChatBuilder();
        var properties = new Dictionary<string, object?> { { "key", "value" } };

        // Act
        builder.WithAdditionalProperties(properties);

        // Assert
        builder.AdditionalProperties.ShouldBe(properties);
    }
}
