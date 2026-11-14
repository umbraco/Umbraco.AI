using System.ComponentModel;
using System.Reflection;

using Umbraco.Abilities.Entities.Models;

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

    // The input's properties as fields, mapped like a model class's (enums as allowed values, other types as JSON).
    private static IEnumerable<FieldDefinition> InputFields(Type inputType)
    {
        var parameters = inputType.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First().GetParameters();
        foreach (var property in inputType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var parameter = parameters.FirstOrDefault(p => string.Equals(p.Name, property.Name, StringComparison.OrdinalIgnoreCase));
            var entityKey = property.GetCustomAttribute<EntityKeyAttribute>() ?? parameter?.GetCustomAttribute<EntityKeyAttribute>();
            var description = property.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;
            var required = parameter is { HasDefaultValue: false } && Nullable.GetUnderlyingType(property.PropertyType) is null;
            yield return ModelShapes.FieldFor(ModelShapes.AliasOf(property), description, property.PropertyType, required, entityKey?.EntityTypes);
        }
    }
}
