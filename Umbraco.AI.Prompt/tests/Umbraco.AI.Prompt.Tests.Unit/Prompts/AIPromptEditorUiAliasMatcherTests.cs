using Shouldly;
using Umbraco.AI.Prompt.Core.Prompts;
using Xunit;

namespace Umbraco.AI.Prompt.Tests.Unit.Prompts;

public class AIPromptEditorUiAliasMatcherTests
{
    [Theory]
    [InlineData("Umb.PropertyEditorUi.MediaPicker", "Umb.PropertyEditorUi.MediaPicker.Single")]
    [InlineData("Umb.PropertyEditorUi.MultiUrlPicker", "Umb.PropertyEditorUi.UrlPicker.Single")]
    [InlineData("Umb.PropertyEditorUi.Dropdown", "Umb.PropertyEditorUi.Dropdown.Single")]
    [InlineData("Umb.PropertyEditorUi.Slider", "Umb.PropertyEditorUi.Slider.Range")]
    [InlineData("Umb.PropertyEditorUi.Label", "Umb.PropertyEditorUi.Label.Integer")]
    [InlineData("Umb.PropertyEditorUi.Label", "Umb.PropertyEditorUi.Label.Text")]
    public void Matches_OldAliasRule_MatchesSplitAlias(string ruleAlias, string actualAlias)
    {
        AIPromptEditorUiAliasMatcher.Matches([ruleAlias], actualAlias).ShouldBeTrue();
    }

    [Theory]
    [InlineData("Umb.PropertyEditorUi.MediaPicker.Single", "Umb.PropertyEditorUi.MediaPicker")]
    [InlineData("Umb.PropertyEditorUi.Dropdown.Single", "Umb.PropertyEditorUi.Dropdown")]
    [InlineData("Umb.PropertyEditorUi.Slider.Range", "Umb.PropertyEditorUi.Slider")]
    public void Matches_NewAliasRule_DoesNotMatchOldAlias(string ruleAlias, string actualAlias)
    {
        AIPromptEditorUiAliasMatcher.Matches([ruleAlias], actualAlias).ShouldBeFalse();
    }

    [Fact]
    public void Matches_NewAliasRule_MatchesItself()
    {
        AIPromptEditorUiAliasMatcher.Matches(["Umb.PropertyEditorUi.UrlPicker.Single"], "Umb.PropertyEditorUi.UrlPicker.Single").ShouldBeTrue();
    }

    [Fact]
    public void Matches_OldAliasRule_StillMatchesOldAlias_IgnoringCase()
    {
        AIPromptEditorUiAliasMatcher.Matches(["umb.propertyeditorui.dropdown"], "Umb.PropertyEditorUi.Dropdown").ShouldBeTrue();
    }

    [Fact]
    public void Matches_OldAliasRule_DoesNotMatchUnrelatedSplitAlias()
    {
        AIPromptEditorUiAliasMatcher.Matches(["Umb.PropertyEditorUi.MediaPicker"], "Umb.PropertyEditorUi.UrlPicker.Single").ShouldBeFalse();
    }

    [Fact]
    public void Matches_AnyRuleAliasMatching_ReturnsTrue()
    {
        AIPromptEditorUiAliasMatcher.Matches(
            ["Umb.PropertyEditorUi.TextBox", "Umb.PropertyEditorUi.Slider"],
            "Umb.PropertyEditorUi.Slider.Range").ShouldBeTrue();
    }
}
