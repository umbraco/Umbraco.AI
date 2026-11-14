using System.ComponentModel;
using System.Reflection;

namespace Umbraco.Abilities.Entities;

/// <summary>
/// Turns abilities into item types, so an entity can hold an items field of ability calls. An Automate workflow is
/// then just an entity whose steps field holds abilities, and any package's new ability becomes a step type.
/// </summary>
public static class AbilityItemTypes
{
    public static ItemTypeDefinition FromAbility(IAbility ability, string group, params FieldDefinition[] extraFields)
    {
        var fields = InputFields(ability.Descriptor.InputType).Concat(extraFields).ToList();
        return new ItemTypeDefinition(group, ability.Descriptor.Id, ability.Descriptor.Name, ability.Descriptor.Description, new ElementShape(fields));
    }

    private static IEnumerable<FieldDefinition> InputFields(Type inputType)
    {
        var parameters = inputType.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First().GetParameters();
        foreach (var property in inputType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var parameter = parameters.FirstOrDefault(p => string.Equals(p.Name, property.Name, StringComparison.OrdinalIgnoreCase));
            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            var entityKey = property.GetCustomAttribute<EntityKeyAttribute>() ?? parameter?.GetCustomAttribute<EntityKeyAttribute>();
            var description = property.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;
            var required = parameter is { HasDefaultValue: false } && Nullable.GetUnderlyingType(property.PropertyType) is null;

            FieldType? fieldType = type switch
            {
                _ when type == typeof(Guid) && entityKey is not null => FieldType.EntityReference,
                _ when type == typeof(string) => FieldType.String,
                _ when type == typeof(bool) => FieldType.Boolean,
                _ when type == typeof(int) || type == typeof(long) => FieldType.Integer,
                _ when type == typeof(decimal) || type == typeof(double) => FieldType.Number,
                _ when type == typeof(DateOnly) => FieldType.Date,
                _ => null,
            };

            if (fieldType is { } ft)
            {
                yield return new FieldDefinition(JsonName(property.Name), description, ft, required)
                {
                    ReferencesEntityType = entityKey?.EntityType,
                };
            }
        }
    }

    private static string JsonName(string name) => char.ToLowerInvariant(name[0]) + name[1..];
}
