using Shouldly;
using Umbraco.AI.Prompt.Core.Prompts;
using Xunit;

namespace Umbraco.AI.Prompt.Tests.Unit.Prompts;

public class AIPromptServiceTemplateContextTests
{
    private static AIPromptExecutionRequest CreateRequest(Guid? elementId = null) => new()
    {
        EntityId = Guid.NewGuid(),
        EntityType = "document",
        PropertyAlias = "title",
        ContentTypeAlias = "homePage",
        ElementId = elementId,
        ElementType = elementId.HasValue ? "block" : null,
    };

    [Fact]
    public void BuildTemplateContext_OnDocument_ExposesNameAndContentTypeAlias()
    {
        var variables = new Dictionary<string, object?> { ["entityName"] = "Home", ["title"] = "Welcome" };

        var context = AIPromptService.BuildTemplateContext(CreateRequest(), variables);

        context["name"].ShouldBe("Home");
        context["contentTypeAlias"].ShouldBe("homePage");
        context["currentValue"].ShouldBe("Welcome");
    }

    [Fact]
    public void BuildTemplateContext_InsideBlock_UsesTheParentEntityName()
    {
        // The element contributor always adds the block's own unprefixed entityName ("Block").
        var variables = new Dictionary<string, object?> { ["entityName"] = "Block", ["entity.entityName"] = "Home" };

        var context = AIPromptService.BuildTemplateContext(CreateRequest(Guid.NewGuid()), variables);

        context["name"].ShouldBe("Home");
    }

    [Fact]
    public void BuildTemplateContext_WithPropertyAliasedName_KeepsThePropertyValue()
    {
        var variables = new Dictionary<string, object?> { ["entityName"] = "Home", ["name"] = "Property value" };

        var context = AIPromptService.BuildTemplateContext(CreateRequest(), variables);

        context["name"].ShouldBe("Property value");
    }
}
