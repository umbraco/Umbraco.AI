using System.Text.Json;
using System.Text.Json.Nodes;

namespace Umbraco.Abilities;

/// <summary>What an ability does. Products derive retry, approval and MCP hints from it.</summary>
public enum AbilityKind
{
    Explain,
    Read,
    Transform,
    Trigger,
    Write,
}

/// <summary>Declares a class as an ability.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AbilityAttribute(string id, string name) : Attribute
{
    public string Id { get; } = id;

    public string Name { get; } = name;

    /// <summary>Written for a model as well as a person: when to use it and what to call first.</summary>
    public string Description { get; set; } = string.Empty;

    public AbilityKind Kind { get; set; } = AbilityKind.Read;

    /// <summary>
    /// A Write that changes no live data, or whose change can be undone (recording into a draft set,
    /// creating or discarding a set). Approval is only needed for a Write that is not reversible.
    /// </summary>
    public bool Reversible { get; set; }
}

/// <summary>Marks a Guid input as the key of an entity of the given type.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class EntityKeyAttribute(string entityType) : Attribute
{
    public string EntityType { get; } = entityType;
}

public sealed record AbilityDescriptor(
    string Id,
    string Name,
    string Description,
    AbilityKind Kind,
    bool Reversible,
    Type InputType,
    Type OutputType)
{
    public bool ChangesData => Kind == AbilityKind.Write;

    public bool RequiresApproval => Kind == AbilityKind.Write && !Reversible;
}

/// <summary>Who is calling, from where, and which draft set (if any) is active.</summary>
public sealed record AbilityContext(string ActingUser, string Caller, Guid? DraftSetId = null);

public enum FailureCategory
{
    /// <summary>The input was wrong. The caller can correct it and try again.</summary>
    Input,

    /// <summary>The acting user may not do this.</summary>
    Access,

    /// <summary>Something failed while running. Retrying the same input will not help.</summary>
    Runtime,
}

/// <summary>A problem at a path. Paths use the same form as the entity edit abilities' paths.</summary>
public sealed record Problem(string Path, string Message);

public sealed record AbilityFailure(FailureCategory Category, string Message, IReadOnlyList<Problem> Problems)
{
    public bool Correctable => Category == FailureCategory.Input;
}

public sealed record AbilityResult(bool Success, object? Output, AbilityFailure? Failure)
{
    public static AbilityResult Ok(object? output) => new(true, output, null);

    public static AbilityResult Fail(FailureCategory category, string message, IReadOnlyList<Problem>? problems = null)
        => new(false, null, new AbilityFailure(category, message, problems ?? []));
}

/// <summary>The standard way an ability points at an entity it created, changed or found.</summary>
public sealed record EntityRef(string EntityType, Guid Key, string Name, string? EditUrl = null);

public interface IAbility
{
    AbilityDescriptor Descriptor { get; }

    Task<AbilityResult> InvokeAsync(JsonObject input, AbilityContext context, CancellationToken cancellationToken = default);
}

/// <summary>Thrown inside an ability to report a correctable input problem.</summary>
public sealed class AbilityInputException(string message, IReadOnlyList<Problem>? problems = null) : Exception(message)
{
    public IReadOnlyList<Problem> Problems { get; } = problems ?? [];
}

public static class AbilityJson
{
    /// <summary>Shared, read-only options with a resolver, so schema generators can use them too.</summary>
    public static readonly JsonSerializerOptions Options = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver(),
        };
        options.MakeReadOnly();
        return options;
    }
}

/// <summary>A stateless ability with a typed input and output.</summary>
public abstract class AbilityBase<TInput, TOutput> : IAbility
{
    protected AbilityBase()
    {
        var attribute = (AbilityAttribute?)Attribute.GetCustomAttribute(GetType(), typeof(AbilityAttribute))
            ?? throw new InvalidOperationException($"{GetType().Name} has no [Ability] attribute.");

        Descriptor = new AbilityDescriptor(
            attribute.Id,
            attribute.Name,
            attribute.Description,
            attribute.Kind,
            attribute.Reversible,
            typeof(TInput),
            typeof(TOutput));
    }

    public AbilityDescriptor Descriptor { get; }

    public async Task<AbilityResult> InvokeAsync(JsonObject input, AbilityContext context, CancellationToken cancellationToken = default)
    {
        TInput? typed;
        try
        {
            typed = input.Deserialize<TInput>(AbilityJson.Options);
        }
        catch (JsonException ex)
        {
            return AbilityResult.Fail(FailureCategory.Input, $"The input does not match the schema: {ex.Message}");
        }

        if (typed is null)
        {
            return AbilityResult.Fail(FailureCategory.Input, "Input is required.");
        }

        try
        {
            return await ExecuteAsync(typed, context, cancellationToken);
        }
        catch (AbilityInputException ex)
        {
            return AbilityResult.Fail(FailureCategory.Input, ex.Message, ex.Problems);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return AbilityResult.Fail(FailureCategory.Runtime, ex.Message);
        }
    }

    protected abstract Task<AbilityResult> ExecuteAsync(TInput input, AbilityContext context, CancellationToken cancellationToken);

    protected static AbilityResult Success(TOutput output) => AbilityResult.Ok(output);
}

public sealed class AbilityRegistry
{
    private readonly Dictionary<string, IAbility> _abilities = new(StringComparer.Ordinal);

    public AbilityRegistry Add(IAbility ability)
    {
        _abilities.Add(ability.Descriptor.Id, ability);
        return this;
    }

    public IAbility? Find(string id) => _abilities.GetValueOrDefault(id);

    public IReadOnlyCollection<IAbility> All => _abilities.Values;
}
