using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using Umbraco.AI.Extensions;
using Umbraco.Cms.Core.Serialization;

namespace Umbraco.AI.Core.EditableModels;

internal sealed class AIEditableModelSchemaBuilder : IAIEditableModelSchemaBuilder
{
    public AIEditableModelSchema BuildForType<TModel>(string modelId)
        where TModel : class
    {
        var modelType = typeof(TModel);
        var properties = modelType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Create an instance to read default values from property initializers
        var modelInstance = Activator.CreateInstance(modelType);

        var fields = properties.Select(property => BuildFieldForProperty(property, modelId, modelInstance)).ToList();
        return new AIEditableModelSchema(modelType, fields);
    }

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
            EditorUiAlias = attr?.EditorUiAlias ?? InferEditorUiAlias(property.PropertyType),
            EditorConfig = attr?.EditorConfig != null
                ? JsonSerializer.Deserialize<JsonElement>(attr.EditorConfig, Constants.DefaultJsonSerializerOptions)
                : null,
            DefaultValue = defaultValue,
            ValidationRules = InferValidationAttributes(property),
            SortOrder = attr?.SortOrder ?? 0,
            IsSensitive = attr?.IsSensitive ?? false,
            Group = attr?.Group ?? $"#uaiFieldGroups_{modelKey}{property.Name}Label"
        };
    }

    private static string InferEditorUiAlias(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType == typeof(string))
            return "Umb.PropertyEditorUi.TextBox";
        if (underlyingType == typeof(int) || underlyingType == typeof(long))
            return "Umb.PropertyEditorUi.Integer";
        if (underlyingType == typeof(bool))
            return "Umb.PropertyEditorUi.Toggle";
        if (underlyingType == typeof(decimal) || underlyingType == typeof(double) || underlyingType == typeof(float))
            return "Umb.PropertyEditorUi.Decimal";
        if (underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset))
            return "Umb.PropertyEditorUi.DatePicker";

        return "Umb.PropertyEditorUi.TextBox";
    }

    private static IEnumerable<ValidationAttribute> InferValidationAttributes(PropertyInfo property)
    {
        var validationAttributes = property.GetCustomAttributes<ValidationAttribute>().ToList();

        // If the property is non-nullable and doesn't already have a Required attribute, add one
        if (!property.IsNullable() && !validationAttributes.OfType<RequiredAttribute>().Any())
        {
            validationAttributes.Add(new RequiredAttribute());
        }

        return validationAttributes;
    }
}
