using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.PropertyEditors;

namespace Umbraco.Ai.Core.PropertyEditors;

/// <summary>
/// Data editor for the AI Context Picker property.
/// </summary>
/// <remarks>
/// Allows content editors to select one or more AI contexts that provide
/// contextual information for AI operations (e.g., brand voice, style guidelines).
/// </remarks>
[DataEditor(
    Constants.PropertyEditors.Aliases.ContextPicker,
    ValueType = ValueTypes.Json,
    ValueEditorIsReusable = true)]
public class AiContextPickerPropertyEditor(IDataValueEditorFactory dataValueEditorFactory, IIOHelper ioHelper)
    : DataEditor(dataValueEditorFactory)
{
    /// <inheritdoc/>
    protected override IConfigurationEditor CreateConfigurationEditor()
        => new AiContextPickerConfigurationEditor(ioHelper);
}