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
    /// A Write that changes no live data, or whose change can be undone (recording into a change set,
    /// creating or discarding a set). Approval is only needed for a Write that is not reversible.
    /// </summary>
    public bool Reversible { get; set; }
}

/// <summary>
/// Marks a Guid as the key of an entity of the given type, or an <c>EntityReference</c> as pointing at one of several
/// entity types.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class EntityKeyAttribute(params string[] entityTypes) : Attribute
{
    public IReadOnlyList<string> EntityTypes { get; } = entityTypes;

    public string EntityType => EntityTypes[0];
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

/// <summary>
/// Who is calling (a backoffice user's key, as the CMS's services take it), from where, and which change set (if any) is
/// active. An agent acts for a user: it gets that user's permissions, no more.
/// </summary>
public sealed record AbilityContext(Guid UserKey, string Caller, Guid? ChangeSetId = null);

/// <summary>
/// How an ability call ended. Mirrors the CMS's operation status enums (e.g. <c>ContentEditingOperationStatus</c>).
/// </summary>
public enum AbilityOperationStatus
{
    Success,

    /// <summary>The input was wrong. The caller can correct it and try again.</summary>
    InvalidInput,

    /// <summary>The acting user may not do this.</summary>
    AccessDenied,

    /// <summary>Something failed while running. Retrying the same input will not help.</summary>
    Failed,
}

/// <summary>
/// A problem at a path. Mirrors the CMS's <c>PropertyValidationError</c>: error messages per culture and segment, at a
/// path. The path is the entity path the edit abilities take (an alias, or a path into items), where the CMS splits it
/// into an alias and a JSON path into the value.
/// </summary>
public sealed record Problem(string Path, IReadOnlyList<string> ErrorMessages)
{
    public Problem(string path, string message)
        : this(path, [message])
    {
    }

    public string? Culture { get; init; }

    public string? Segment { get; init; }

    /// <summary>The error messages as one sentence.</summary>
    public override string ToString() => string.Join(" ", ErrorMessages);
}

/// <summary>
/// The result of an ability call. Mirrors the CMS's <c>Attempt&lt;TResult, TStatus&gt;</c>: a status and a result, plus a
/// message and problems when it failed.
/// </summary>
public sealed record AbilityResult(AbilityOperationStatus Status, object? Result, string? Message = null, IReadOnlyList<Problem>? Problems = null)
{
    public bool Success => Status == AbilityOperationStatus.Success;

    /// <summary>The caller can correct the input and try again.</summary>
    public bool Correctable => Status == AbilityOperationStatus.InvalidInput;

    public static AbilityResult Ok(object? result) => new(AbilityOperationStatus.Success, result);

    public static AbilityResult Fail(AbilityOperationStatus status, string message, IReadOnlyList<Problem>? problems = null)
        => new(status, null, message, problems ?? []);
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
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
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
            return AbilityResult.Fail(AbilityOperationStatus.InvalidInput, $"The input does not match the schema: {ex.Message}");
        }

        if (typed is null)
        {
            return AbilityResult.Fail(AbilityOperationStatus.InvalidInput, "Input is required.");
        }

        try
        {
            return await ExecuteAsync(typed, context, cancellationToken);
        }
        catch (AbilityInputException ex)
        {
            return AbilityResult.Fail(AbilityOperationStatus.InvalidInput, ex.Message, ex.Problems);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return AbilityResult.Fail(AbilityOperationStatus.Failed, ex.Message);
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
