using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Cms.Core.Models;
using Umbraco.Deploy.Core.Connectors.ValueConnectors;

namespace Umbraco.AI.Deploy.Connectors.ValueConnectors;

/// <summary>
/// Value connector for AI Context Picker property editor.
/// Converts stored context GUIDs to UDIs for deployment.
/// </summary>
public class UmbracoAIContextPickerValueConnector : ValueConnectorBase
{
    /// <inheritdoc />
    public override IEnumerable<string> PropertyEditorAliases => ["Uai.ContextPicker"];

    /// <inheritdoc />
    public override Task<string?> ToArtifactAsync(
        object? value,
        IPropertyType propertyType,
        ICollection<ArtifactDependency> dependencies,
        IContextCache contextCache,
        CancellationToken cancellationToken = default)
    {
        var svalue = value as string;

        if (string.IsNullOrWhiteSpace(svalue))
        {
            return Task.FromResult<string?>(null);
        }

        if (!Guid.TryParse(svalue, out Guid contextId))
        {
            return Task.FromResult<string?>(null);
        }

        // TODO: Validate context exists via IAIContextService (when Context feature is fully implemented)
        // For now, create UDI without validation

        var udi = new GuidUdi(UmbracoAIConstants.UdiEntityType.Context, contextId);
        dependencies.Add(new UmbracoAIArtifactDependency(udi, ArtifactDependencyMode.Match));

        return Task.FromResult<string?>(udi.ToString());
    }

    /// <inheritdoc />
    public override Task<object?> FromArtifactAsync(
        string? value,
        IPropertyType propertyType,
        object? currentValue,
        IContextCache contextCache,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Task.FromResult<object?>(null);
        }

        // Try to parse as UDI first (artifact format)
        if (UdiParser.TryParse(value, out Udi? udi) && udi is GuidUdi guidUdi)
        {
            // TODO: Validate context exists via IAIContextService (when Context feature is fully implemented)
            // For now, return GUID directly
            return Task.FromResult<object?>(guidUdi.Guid.ToString());
        }

        // Fallback: try to parse as GUID directly (legacy format)
        if (Guid.TryParse(value, out _))
        {
            return Task.FromResult<object?>(value);
        }

        return Task.FromResult<object?>(null);
    }
}
