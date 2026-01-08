using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Umbraco.Ai.Extensions;

namespace Umbraco.Ai.Core.EditableModels;

internal sealed class AiEditableModelSchemaBuilder : IAiEditableModelSchemaBuilder
{
    public AiEditableModelSchema BuildForType<TModel>(string modelId)
        where TModel : class
    {
        var properties = typeof(TModel)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var fields = properties.Select(property => BuildFieldForProperty(property, modelId)).ToList();
        return new AiEditableModelSchema(typeof(TModel), fields);
    }

    private AiEditableModelField BuildFieldForProperty(PropertyInfo property, string modelId)
    {
        var attr = property.GetCustomAttribute<AiEditableModelFieldAttribute>();
        var key = property.Name.ToCamelCase();
        var modelKey = modelId.ToCamelCase();

        return new AiEditableModelField
        {
            Key = key,
            PropertyName = property.Name,
            PropertyType = property.PropertyType,
            Label = attr?.Label ?? $"#uaiFields_{modelKey}{property.Name}Label",
            Description = attr?.Description ?? $"#uaiFields_{modelKey}{property.Name}Description",
            EditorUiAlias = attr?.EditorUiAlias ?? InferEditorUiAlias(property.PropertyType),
            DefaultValue = attr?.DefaultValue,
            ValidationRules = InferValidationAttributes(property),
            SortOrder = attr?.SortOrder ?? 0
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
