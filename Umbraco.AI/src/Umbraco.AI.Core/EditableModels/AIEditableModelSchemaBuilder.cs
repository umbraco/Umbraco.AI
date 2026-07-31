using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using Umbraco.AI.Extensions;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Extensions;

namespace Umbraco.AI.Core.EditableModels;

internal sealed class AIEditableModelSchemaBuilder : IAIEditableModelSchemaBuilder
{
    public AIEditableModelSchema BuildForType(Type modelType, string modelId)
    {
        var properties = modelType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Create an instance to read default values from property initializers
        var modelInstance = Activator.CreateInstance(modelType);

        var fields = properties.Select(property => BuildFieldForProperty(property, modelId, modelInstance)).ToList();
        return new AIEditableModelSchema(modelType, fields);
    }

    public AIEditableModelSchema BuildForType<TModel>(string modelId)
        where TModel : class
        => BuildForType(typeof(TModel), modelId);

    private AIEditableModelField BuildFieldForProperty(PropertyInfo property, string modelId, object? modelInstance)
    {
        var attr = property.GetCustomAttribute<AIEditableModelFieldAttribute>();
        var key = property.Name.ToCamelCase();
        var modelKey = modelId.ToCamelCase();

        // Read default value from the model instance's property initializer
        object? defaultValue = null;
        if (modelInstance != null && property.CanRead)
        {
            try
            {
                defaultValue = property.GetValue(modelInstance);
            }
            catch
            {
                // If we can't read the property value, just leave it as null
            }
        }

        return new AIEditableModelField
        {
            Key = key,
            PropertyName = property.Name,
            PropertyType = property.PropertyType,
            Label = attr?.Label ?? $"#uaiFields_{modelKey}{property.Name}Label",
            Description = attr?.Description ?? $"#uaiFields_{modelKey}{property.Name}Description",
            EditorUiAlias = attr?.EditorUiAlias ?? InferEditorUiAlias(property.PropertyType, attr?.IsSensitive ?? false),
            EditorConfig = attr?.EditorConfig != null
                ? JsonSerializer.Deserialize<JsonElement>(attr.EditorConfig, Constants.DefaultJsonSerializerOptions)
                : null,
            DefaultValue = defaultValue,
            ValidationRules = InferValidationAttributes(property),
            SortOrder = attr?.SortOrder ?? 0,
            IsSensitive = attr?.IsSensitive ?? false,
            Group = (attr?.Group).IsNullOrWhiteSpace() ? null :
                attr.Group.StartsWith("#") ? attr.Group :  $"#uaiFieldGroups_{attr.Group.ToCamelCase()}Label"
        };
    }

    /// <summary>
    /// Picks the editor to render a field with when its <see cref="AIEditableModelFieldAttribute"/>
    /// does not name one explicitly.
    /// </summary>
    /// <remarks>
    /// Sensitive strings default to a masked editor (with a reveal toggle) so credentials are not
    /// left on screen during screen shares and demos. This is inferred here rather than flagged to
    /// the client, so <c>IsSensitive</c> never has to cross the API boundary — the same approach
    /// already taken for <c>IsRequired</c>. A settings author who wants a sensitive field rendered
    /// some other way sets <see cref="AIEditableModelFieldAttribute.EditorUiAlias"/>, which is
    /// checked first by the caller and so always wins.
    /// </remarks>
    private static string InferEditorUiAlias(Type type, bool isSensitive)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType == typeof(string))
            return isSensitive ? "Uai.PropertyEditorUi.MaskedTextBox" : "Umb.PropertyEditorUi.TextBox";
        if (underlyingType == typeof(int) || underlyingType == typeof(long))
            return "Umb.PropertyEditorUi.Integer";
        if (underlyingType == typeof(bool))
            return "Umb.PropertyEditorUi.Toggle";
        if (underlyingType == typeof(decimal) || underlyingType == typeof(double) || underlyingType == typeof(float))
            return "Umb.PropertyEditorUi.Decimal";
        if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset))
            return "Umb.PropertyEditorUi.DatePicker";

        // Anything else also falls back to a text box, so keep the sensitive/masked pairing here too.
        return isSensitive ? "Uai.PropertyEditorUi.MaskedTextBox" : "Umb.PropertyEditorUi.TextBox";
    }

    private static IEnumerable<ValidationAttribute> InferValidationAttributes(PropertyInfo property)
    {
        var validationAttributes = property.GetCustomAttributes<ValidationAttribute>().ToList();

        // If the property is non-nullable and doesn't already have a Required attribute, add one.
        // Skip value types (bool, int, etc.) since they always have a default value.
        if (!property.IsNullable()
            && !property.PropertyType.IsValueType
            && !validationAttributes.OfType<RequiredAttribute>().Any())
        {
            validationAttributes.Add(new RequiredAttribute());
        }

        return validationAttributes;
    }
}
