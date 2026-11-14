using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Umbraco.Abilities.Samples.Automate.Plugins;

// A stand-in for Automate's plugin model, shaped like what Umbraco.Commerce.Automate uses: [Trigger] and [Action]
// attributes with required sections, and typed settings classes whose properties carry [Field]. Packages register their
// triggers and actions in collections. Nothing here knows about abilities.

[AttributeUsage(AttributeTargets.Class)]
public sealed class TriggerAttribute(string alias, string name) : Attribute
{
    public string Alias { get; } = alias;

    public string Name { get; } = name;

    public string? Description { get; set; }

    public string[] RequiredSections { get; set; } = [];
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class ActionAttribute(string alias, string name) : Attribute
{
    public string Alias { get; } = alias;

    public string Name { get; } = name;

    public string? Description { get; set; }

    public string[] RequiredSections { get; set; } = [];
}

[AttributeUsage(AttributeTargets.Property)]
public sealed class FieldAttribute : Attribute
{
    public string? Label { get; set; }

    public string? Description { get; set; }

    public string? EditorUiAlias { get; set; }

    public int SortOrder { get; set; }

    /// <summary>The value can be a binding to an earlier step's output instead of a literal.</summary>
    public bool SupportsBindings { get; set; }
}

public abstract class TriggerBase<TSettings>;

public abstract class ActionBase<TSettings>;

/// <summary>A registered trigger or action, as Automate describes it.</summary>
public sealed record AutomatePlugin(string Alias, string Name, string? Description, IReadOnlyList<string> RequiredSections, IReadOnlyList<AutomateField> Fields);

public sealed record AutomateField(string Key, FieldAttribute Field, Type PropertyType, IReadOnlyList<ValidationAttribute> Rules);

/// <summary>Registered triggers or actions. Packages add theirs.</summary>
public sealed class AutomatePluginCollection : List<Type>
{
    public IEnumerable<AutomatePlugin> Plugins => this.Select(Describe);

    private static AutomatePlugin Describe(Type type)
    {
        var (alias, name, description, sections) = type.GetCustomAttribute<TriggerAttribute>() is { } trigger
            ? (trigger.Alias, trigger.Name, trigger.Description, trigger.RequiredSections)
            : type.GetCustomAttribute<ActionAttribute>() is { } action
                ? (action.Alias, action.Name, action.Description, action.RequiredSections)
                : throw new InvalidOperationException($"{type.Name} is not a trigger or action.");

        var settings = type.BaseType!.GetGenericArguments()[0];
        var fields = settings.GetProperties()
            .Select(p => (Property: p, Field: p.GetCustomAttribute<FieldAttribute>()))
            .Where(p => p.Field is not null)
            .OrderBy(p => p.Field!.SortOrder)
            .Select(p => new AutomateField(char.ToLowerInvariant(p.Property.Name[0]) + p.Property.Name[1..], p.Field!, p.Property.PropertyType, p.Property.GetCustomAttributes<ValidationAttribute>().ToList()))
            .ToList();

        return new AutomatePlugin(alias, name, description, sections, fields);
    }
}

// ── Automate's own triggers ─────────────────────────────────────────────────────────────────────────────────

public sealed class FormSubmittedSettings
{
    [Field(Label = "Form", Description = "The form's name.")]
    [Required]
    public string? Form { get; set; }
}

[Trigger("formSubmitted", "Form submitted", Description = "Runs when a form is submitted.")]
public sealed class FormSubmittedTrigger : TriggerBase<FormSubmittedSettings>;

public sealed class ScheduleSettings
{
    [Field(Label = "Cron", Description = "A cron expression.")]
    [Required]
    public string? Cron { get; set; }
}

[Trigger("schedule", "Schedule", Description = "Runs on a schedule.")]
public sealed class ScheduleTrigger : TriggerBase<ScheduleSettings>;
