using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Settings;

internal sealed class AiSettingDefinitionBuilder : IAiSettingDefinitionBuilder
{
    public IReadOnlyList<AiSettingDefinition> BuildForType<TSettings>(string providerId)
        where TSettings : class
    {
        var properties = typeof(TSettings)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);
        return properties.Select(property => BuildForProperty(property, providerId)).ToList();
    }

    private AiSettingDefinition BuildForProperty(PropertyInfo property, string providerId)
    {
        var attr = property.GetCustomAttribute<AiSettingAttribute>();
        var key = property.Name.ToCamelCase();
        var providerKey = providerId.ToCamelCase();

        return new AiSettingDefinition
        {
            Key = key,
            PropertyName = property.Name,
            PropertyType = property.PropertyType,
            Label = attr?.Label ?? $"#umbracoAiProviders_{providerKey}Settings{property.Name}Label",
            Description = attr?.Description ?? $"#umbracoAiProviders_{providerKey}Settings{property.Name}Description",
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

        // if (underlyingType.IsEnum) return "Umb.PropertyEditorUi.Dropdown";

        return "Umb.PropertyEditorUi.TextBox";
    }
    
    private static IEnumerable<ValidationAttribute> InferValidationAttributes(PropertyInfo property)
    {
        var validationAttributes = property.GetCustomAttributes<ValidationAttribute>().ToList();
        
        // If the property is non-nullable and doesn't already have a Required attribute, add one
        if (!property.PropertyType.IsNullable() && !validationAttributes.OfType<RequiredAttribute>().Any())
        {
            validationAttributes.Add(new RequiredAttribute());
        }

        return validationAttributes;
    }
}