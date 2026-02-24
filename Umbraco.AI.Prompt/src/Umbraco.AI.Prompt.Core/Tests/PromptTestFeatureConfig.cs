using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Tests;

namespace Umbraco.AI.Prompt.Core.Tests;

/// <summary>
/// Configuration for prompt test feature.
/// Defines mock entity context and prompt-specific settings for test execution.
/// </summary>
public class PromptTestFeatureConfig : AITestFeatureConfigBase
{
    /// <summary>
    /// The property alias being edited.
    /// </summary>
    [AIField(
        Label = "Property Alias",
        Description = "The property being edited by the prompt",
        EditorUiAlias = "Umb.PropertyEditorUi.TextBox",
        Group = "Context",
        SortOrder = -11)]
    [Required]
    public string PropertyAlias { get; set; } = string.Empty;
}
