using System.ComponentModel.DataAnnotations;

using Umbraco.Abilities.Entities.Models;

namespace Umbraco.Abilities.Entities;

/// <summary>
/// One setting of a plugin, as the product's registry describes it: Commerce's setting definitions, Automate's
/// <c>[Field]</c> settings. A product maps its own metadata to this; <see cref="ItemTypeFields"/> turns it into a field.
/// </summary>
public sealed record SettingDescriptor(
    string Key,
    string? Label,
    string? Description,
    Type ClrType,
    IReadOnlyList<ValidationAttribute> Rules,
    string? EditorUiAlias = null,
    int SortOrder = 0,
    bool AcceptsBindings = false);

/// <summary>Builds item type fields from a product's setting metadata, with the same type mapping as model classes.</summary>
public static class ItemTypeFields
{
    /// <summary>
    /// Text meant for a person, or null when there is none: products often leave a localization key (<c>#uc…Label</c>)
    /// that only the backoffice resolves, which tells a model nothing.
    /// </summary>
    public static string? Text(string? value) => string.IsNullOrWhiteSpace(value) || value.StartsWith('#') ? null : value;

    /// <summary>
    /// A field for a setting. <paramref name="editorReferences"/> is the product's table from an editor UI alias to the
    /// entity types it picks (a store picker picks stores). An editor it doesn't know leaves a plain value, and the
    /// description names the editor, so the field still works with less guidance.
    /// </summary>
    public static FieldDefinition For(SettingDescriptor setting, IReadOnlyDictionary<string, string[]> editorReferences)
    {
        var references = setting.EditorUiAlias is { } editor ? editorReferences.GetValueOrDefault(editor) : null;
        // A picker that stores a key as a string is a reference to what it picks.
        var type = references is not null && setting.ClrType == typeof(string) ? typeof(Guid?) : setting.ClrType;
        var allowed = setting.Rules.OfType<AllowedValuesAttribute>().FirstOrDefault()?.Values.Select(v => v?.ToString() ?? string.Empty).ToList();

        var description = string.Join(" ", new[] { Text(setting.Label), Text(setting.Description) }.OfType<string>().Distinct());
        if (setting.EditorUiAlias is { } unknown && references is null && !IsPlainEditor(unknown))
        {
            description = $"{description} Edited with '{unknown}' in the backoffice.".Trim();
        }

        if (setting.AcceptsBindings)
        {
            description = $"{description} Can be a binding to an earlier step's output, e.g. {{{{trigger.orderId}}}}.".Trim();
        }

        var field = ModelShapes.FieldFor(setting.Key, description, type, setting.Rules.OfType<RequiredAttribute>().Any(), references, allowed);
        return field with
        {
            Rules = setting.Rules.Where(r => r is not RequiredAttribute and not AllowedValuesAttribute).ToList(),
            AcceptsBindings = setting.AcceptsBindings,
        };
    }

    private static bool IsPlainEditor(string editorUiAlias)
        => editorUiAlias is "Umb.PropertyEditorUi.TextBox" or "Umb.PropertyEditorUi.Toggle" or "Umb.PropertyEditorUi.Decimal" or "Umb.PropertyEditorUi.Integer";
}
