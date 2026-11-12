using System.Reflection;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Anthropic.Tests.Unit;

public class AnthropicSettingsDeclarationTests
{
    [Theory]
    [InlineData(nameof(AnthropicChatCapabilitySettings.Effort))]
    [InlineData(nameof(AnthropicChatCapabilitySettings.PromptCaching))]
    public void SingleChoiceSettings_UseTheSingleChoiceDropdown(string property)
    {
        // CMS 19's plain Dropdown UI is always multi-select; single choice is its own UI alias.
        var field = typeof(AnthropicChatCapabilitySettings).GetProperty(property)!.GetCustomAttribute<AIFieldAttribute>()!;

        field.EditorUiAlias.ShouldBe("Umb.PropertyEditorUi.Dropdown.Single");
    }
}
