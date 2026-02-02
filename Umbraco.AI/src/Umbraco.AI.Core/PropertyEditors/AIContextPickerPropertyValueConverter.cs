using System.Text.Json;
using Umbraco.AI.Core.Contexts;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;

namespace Umbraco.AI.Core.PropertyEditors;

/// <summary>
/// Converts AI Context Picker property values to strongly typed <see cref="AIContext"/> instances.
/// </summary>
/// <remarks>
/// <para>
/// The property value is stored as JSON:
/// <list type="bullet">
/// <item><description>Single selection (multiple = false): <c>"guid1"</c></description></item>
/// <item><description>Multiple selection (multiple = true): <c>["guid1", "guid2"]</c></description></item>
/// </list>
/// </para>
/// <para>
/// When "Allow Multiple" is enabled, returns <see cref="IEnumerable{AIContext}"/>.
/// When disabled, returns a single <see cref="AIContext"/> or null.
/// </para>
/// </remarks>
public class AIContextPickerPropertyValueConverter : PropertyValueConverterBase
{
    private readonly IAiContextService _contextService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIContextPickerPropertyValueConverter"/> class.
    /// </summary>
    /// <param name="contextService">The AI context service.</param>
    public AIContextPickerPropertyValueConverter(IAiContextService contextService)
    {
        _contextService = contextService;
    }

    /// <inheritdoc />
    public override bool IsConverter(IPublishedPropertyType propertyType)
        => propertyType.EditorAlias == Constants.PropertyEditors.Aliases.ContextPicker;

    /// <inheritdoc />
    public override Type GetPropertyValueType(IPublishedPropertyType propertyType)
    {
        var isMultiple = IsMultipleSelection(propertyType);
        return isMultiple
            ? typeof(IEnumerable<AIContext>)
            : typeof(AIContext);
    }

    /// <inheritdoc />
    public override PropertyCacheLevel GetPropertyCacheLevel(IPublishedPropertyType propertyType)
        => PropertyCacheLevel.Element;

    /// <inheritdoc />
    public override object? ConvertSourceToIntermediate(
        IPublishedElement owner,
        IPublishedPropertyType propertyType,
        object? source,
        bool preview)
    {
        return source switch
        {
            // Handle null source
            null => null,
            // Handle the case where the source is already a Guid array
            Guid[] ids => ids,
            // Handle single Guid value (when multiple = false)
            Guid singleId => new[] { singleId },
            // Handle JSON string
            string json when !string.IsNullOrWhiteSpace(json) => ParseJsonValue(json),
            // Unhandled source type
            _ => null
        };
    }

    /// <summary>
    /// Parses a JSON value that could be either a single GUID or an array of GUIDs.
    /// </summary>
    private static Guid[]? ParseJsonValue(string json)
    {
        // Try to parse as single GUID string first (when multiple = false)
        if (json.StartsWith('"') && json.EndsWith('"'))
        {
            var guidString = json.Trim('"');
            if (Guid.TryParse(guidString, out var singleGuid))
            {
                return new[] { singleGuid };
            }
        }

        // Try to parse as GUID directly (unquoted single value)
        if (Guid.TryParse(json, out var directGuid))
        {
            return new[] { directGuid };
        }

        // Try to parse as array of GUIDs (when multiple = true)
        try
        {
            return JsonSerializer.Deserialize<Guid[]>(json);
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public override object? ConvertIntermediateToObject(
        IPublishedElement owner,
        IPublishedPropertyType propertyType,
        PropertyCacheLevel referenceCacheLevel,
        object? inter,
        bool preview)
    {
        if (inter is not Guid[] ids || ids.Length == 0)
        {
            return IsMultipleSelection(propertyType)
                ? Enumerable.Empty<AIContext>()
                : null;
        }

        var isMultiple = IsMultipleSelection(propertyType);

        // Look up contexts synchronously (required by IPropertyValueConverter interface)
        var contexts = new List<AIContext>();
        foreach (var id in ids)
        {
            var context = _contextService.GetContextAsync(id).GetAwaiter().GetResult();
            if (context != null)
            {
                contexts.Add(context);
            }
        }

        return isMultiple
            ? contexts.AsEnumerable()
            : contexts.FirstOrDefault();
    }

    private static bool IsMultipleSelection(IPublishedPropertyType propertyType)
    {
        // Check the "multiple" configuration value from the data type configuration
        var configObject = propertyType.DataType.ConfigurationObject;
        if (configObject is Dictionary<string, object> config
            && config.TryGetValue("multiple", out var multipleValue))
        {
            return multipleValue switch
            {
                bool b => b,
                string s => bool.TryParse(s, out var result) && result,
                _ => false
            };
        }

        return false;
    }
}
