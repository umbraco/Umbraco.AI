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
    public void WithGuardrails_StoresAdditionalIds()
    {
        // Arrange
        var guardrailId = Guid.NewGuid();
        var builder = new AIChatBuilder();
        builder.WithGuardrails(guardrailId);

        // Assert
        builder.AdditionalGuardrailIds.ShouldContain(guardrailId);
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

    [Fact]
    public void WithContexts_ById_StoresAdditionalIds()
    {
        // Arrange
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var builder = new AIChatBuilder();

        // Act
        builder.WithContexts(a, b);

        // Assert
        builder.AdditionalContextIds.ShouldBe([a, b]);
        builder.AdditionalContextAliases.ShouldBeNull();
        builder.ContextIds.ShouldBeNull();
    }

    [Fact]
    public void WithContexts_ByAlias_StoresAdditionalAliases()
    {
        // Arrange
        var builder = new AIChatBuilder();

        // Act
        builder.WithContexts("brand", "guidelines");

        // Assert
        builder.AdditionalContextAliases.ShouldBe(["brand", "guidelines"]);
        builder.AdditionalContextIds.ShouldBeEmpty();
    }

    [Fact]
    public void SetContexts_ById_StoresReplaceContextIds()
    {
        // Arrange
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var builder = new AIChatBuilder();

        // Act
        builder.SetContexts(a, b);

        // Assert
        builder.ContextIds.ShouldBe([a, b]);
        builder.ContextAliases.ShouldBeNull();
    }

    [Fact]
    public void SetContexts_EmptyArray_SignalsExplicitNoContexts()
    {
        // Arrange
        var builder = new AIChatBuilder();

        // Act
        builder.SetContexts(Array.Empty<Guid>());

        // Assert
        builder.ContextIds.ShouldNotBeNull();
        builder.ContextIds!.Count.ShouldBe(0);
    }

    [Fact]
    public void SetContexts_ByAlias_ThenById_ClearsAliases()
    {
        // Arrange
        var id = Guid.NewGuid();
        var builder = new AIChatBuilder();
        builder.SetContexts("alias-one");

        // Act
        builder.SetContexts(id);

        // Assert
        builder.ContextIds.ShouldBe([id]);
        builder.ContextAliases.ShouldBeNull();
    }

    [Fact]
    public void PopulateContext_SetContexts_WritesOverrideKey()
    {
        // Arrange
        var id = Guid.NewGuid();
        var builder = new AIChatBuilder();
        builder.WithAlias("test").SetContexts(id);
        var runtimeContext = new AIRuntimeContext([]);

        // Act
        builder.PopulateContext(runtimeContext, setFeatureMetadata: true);

        // Assert
        runtimeContext.GetValue<IReadOnlyList<Guid>>(Core.Constants.ContextKeys.ContextIdsOverride)
            .ShouldBe([id]);
        runtimeContext.GetValue<IReadOnlyList<Guid>>(Core.Constants.ContextKeys.AdditionalContextIds).ShouldBeNull();
    }

    [Fact]
    public void PopulateContext_WithContexts_WritesAdditionalKey()
    {
        // Arrange
        var id = Guid.NewGuid();
        var builder = new AIChatBuilder();
        builder.WithAlias("test").WithContexts(id);
        var runtimeContext = new AIRuntimeContext([]);

        // Act
        builder.PopulateContext(runtimeContext, setFeatureMetadata: true);

        // Assert
        runtimeContext.GetValue<IReadOnlyList<Guid>>(Core.Constants.ContextKeys.AdditionalContextIds)
            .ShouldBe([id]);
        runtimeContext.GetValue<IReadOnlyList<Guid>>(Core.Constants.ContextKeys.ContextIdsOverride).ShouldBeNull();
    }

    [Fact]
    public void PopulateContext_SetAndWithContexts_WritesBothKeys()
    {
        // Arrange
        var replaceId = Guid.NewGuid();
        var additionalId = Guid.NewGuid();
        var builder = new AIChatBuilder();
        builder.WithAlias("test").SetContexts(replaceId).WithContexts(additionalId);
        var runtimeContext = new AIRuntimeContext([]);

        // Act
        builder.PopulateContext(runtimeContext, setFeatureMetadata: true);

        // Assert — both keys emitted; resolver combines them.
        runtimeContext.GetValue<IReadOnlyList<Guid>>(Core.Constants.ContextKeys.ContextIdsOverride)
            .ShouldBe([replaceId]);
        runtimeContext.GetValue<IReadOnlyList<Guid>>(Core.Constants.ContextKeys.AdditionalContextIds)
            .ShouldBe([additionalId]);
    }

    [Fact]
    public void WithGuardrails_ById_StoresAdditionalIds()
    {
        var id = Guid.NewGuid();
        var builder = new AIChatBuilder();

        builder.WithGuardrails(id);

        builder.AdditionalGuardrailIds.ShouldBe([id]);
        builder.AdditionalGuardrailAliases.ShouldBeNull();
    }

    [Fact]
    public void WithGuardrails_ByAlias_StoresAdditionalAliases()
    {
        var builder = new AIChatBuilder();

        builder.WithGuardrails("safety");

        builder.AdditionalGuardrailAliases.ShouldBe(["safety"]);
        builder.AdditionalGuardrailIds.ShouldBeEmpty();
    }

    [Fact]
    public void SetGuardrails_ById_StoresReplaceGuardrailIds()
    {
        var id = Guid.NewGuid();
        var builder = new AIChatBuilder();

        builder.SetGuardrails(id);

        builder.GuardrailIds.ShouldBe([id]);
        builder.GuardrailAliases.ShouldBeNull();
    }

    [Fact]
    public void PopulateContext_WithGuardrails_WritesAdditionalKey()
    {
        var id = Guid.NewGuid();
        var builder = new AIChatBuilder();
        builder.WithAlias("test").WithGuardrails(id);
        var runtimeContext = new AIRuntimeContext([]);

        builder.PopulateContext(runtimeContext, setFeatureMetadata: true);

        runtimeContext.GetValue<IReadOnlyList<Guid>>(Core.Constants.ContextKeys.AdditionalGuardrailIds)
            .ShouldBe([id]);
        runtimeContext.GetValue<IReadOnlyList<Guid>>(Core.Constants.ContextKeys.GuardrailIdsOverride)
            .ShouldBeNull();
    }

    [Fact]
    public void PopulateContext_SetGuardrails_WritesOverrideKey()
    {
        var id = Guid.NewGuid();
        var builder = new AIChatBuilder();
        builder.WithAlias("test").SetGuardrails(id);
        var runtimeContext = new AIRuntimeContext([]);

        builder.PopulateContext(runtimeContext, setFeatureMetadata: true);

        runtimeContext.GetValue<IReadOnlyList<Guid>>(Core.Constants.ContextKeys.GuardrailIdsOverride)
            .ShouldBe([id]);
        runtimeContext.GetValue<IReadOnlyList<Guid>>(Core.Constants.ContextKeys.AdditionalGuardrailIds)
            .ShouldBeNull();
    }

    [Fact]
    public void PopulateContext_SetAndWithGuardrails_WritesBothKeys()
    {
        var replaceId = Guid.NewGuid();
        var additionalId = Guid.NewGuid();
        var builder = new AIChatBuilder();
        builder.WithAlias("test").SetGuardrails(replaceId).WithGuardrails(additionalId);
        var runtimeContext = new AIRuntimeContext([]);

        builder.PopulateContext(runtimeContext, setFeatureMetadata: true);

        runtimeContext.GetValue<IReadOnlyList<Guid>>(Core.Constants.ContextKeys.GuardrailIdsOverride)
            .ShouldBe([replaceId]);
        runtimeContext.GetValue<IReadOnlyList<Guid>>(Core.Constants.ContextKeys.AdditionalGuardrailIds)
            .ShouldBe([additionalId]);
    }
}
