using System.Text.Json;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Base class for test feature configurations.
/// Provides standard entity context and context override properties
/// that all test features share.
/// </summary>
public class AITestFeatureConfigBase
{
    /// <summary>
    /// Mock entity context configuration (entity type, sub-type, and mock entity data).
    /// </summary>
    [AIField(
        Label = "Entity Context",
        Description = "Mock entity data to test with",
        EditorUiAlias = "Uai.PropertyEditorUi.MockEntity",
        Group = "Context",
        SortOrder = -20)]
    public virtual JsonElement? EntityContext { get; set; }

}
