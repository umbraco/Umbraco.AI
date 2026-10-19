using Shouldly;
using Umbraco.AI.Prompt.Core.Templates;
using Xunit;

namespace Umbraco.AI.Prompt.Tests.Unit.Templates;

public class AITemplateExpressionTests
{
    [Theory]
    [InlineData("Improve this: {{selection}}")]
    [InlineData("Improve this: {{ selection }}")]
    [InlineData("Improve this: {{\tselection\n}}")]
    [InlineData("Improve this: {{Selection}}")]
    [InlineData("Improve this: {{text:selection}}")]
    [InlineData("Intro {{title}} then {{selection}}")]
    [InlineData("{{selection}} and {{selection}}")]
    public void ReferencesSelection_WhenInstructionsUseSelection_ReturnsTrue(string template)
    {
        AITemplateExpression.ReferencesSelection(template).ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Write a short intro for {{title}}")]
    [InlineData("The word selection on its own")]
    [InlineData("{selection}")]
    [InlineData("{{selections}}")]
    [InlineData("{{my.selection}}")]
    [InlineData("{{image:selection}}")]
    [InlineData("{{selection")]
    public void ReferencesSelection_WhenInstructionsDoNotUseSelection_ReturnsFalse(string? template)
    {
        AITemplateExpression.ReferencesSelection(template).ShouldBeFalse();
    }
}
