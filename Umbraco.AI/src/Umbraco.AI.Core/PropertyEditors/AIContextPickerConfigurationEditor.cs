using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.PropertyEditors;

namespace Umbraco.Ai.Core.PropertyEditors;

/// <summary>
/// Configuration editor for the AI Context Picker property editor.
/// </summary>
public class AiContextPickerConfigurationEditor : ConfigurationEditor<AiContextPickerConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AiContextPickerConfigurationEditor"/> class.
    /// </summary>
    /// <param name="ioHelper">The I/O helper.</param>
    public AiContextPickerConfigurationEditor(IIOHelper ioHelper)
        : base(ioHelper)
    { }
}
