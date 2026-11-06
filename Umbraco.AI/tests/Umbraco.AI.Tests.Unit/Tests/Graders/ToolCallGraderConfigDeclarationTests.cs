using System.Reflection;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Tests.Graders;

namespace Umbraco.AI.Tests.Unit.Tests.Graders;

public class ToolCallGraderConfigDeclarationTests
{
    [Fact]
    public void ValidationMode_UsesTheSingleChoiceDropdown()
    {
        // CMS 19's plain Dropdown UI is always multi-select; single choice is its own UI alias.
        var field = typeof(ToolCallGraderConfig)
            .GetProperty(nameof(ToolCallGraderConfig.ValidationMode))!
            .GetCustomAttribute<AIFieldAttribute>()!;

        field.EditorUiAlias.ShouldBe("Umb.PropertyEditorUi.Dropdown.Single");
    }
}
