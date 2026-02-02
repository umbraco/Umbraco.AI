using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.PropertyEditors;

namespace Umbraco.AI.Core.PropertyEditors;

/// <summary>
/// Configuration editor for the AI Context Picker property editor.
/// </summary>
public class AIContextPickerConfigurationEditor : ConfigurationEditor<AIContextPickerConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIContextPickerConfigurationEditor"/> class.
    /// </summary>
    /// <param name="ioHelper">The I/O helper.</param>
    public AIContextPickerConfigurationEditor(IIOHelper ioHelper)
        : base(ioHelper)
    { }
}
