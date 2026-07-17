using System.Linq;

namespace Umbraco.AI.Core.EditableModels;

/// <summary>
/// Service for resolving editable models from various storage formats.
/// Handles JSON deserialization, configuration variable substitution, and validation.
/// </summary>
public interface IAIEditableModelResolver
{
    /// <summary>
    /// Resolves a model from storage format (JsonElement, Dictionary, etc.) to a typed instance.
    /// Supports configuration variable substitution using the pattern <c>$Key:Path</c>, restricted
    /// to keys under an allowed prefix (see <c>AIOptions.AllowedConfigurationKeyPrefixes</c>).
    /// Validates the resolved model using the schema's validation rules when a schema is provided.
    /// </summary>
    /// <typeparam name="TModel">The type of model to resolve to.</typeparam>
    /// <param name="data">The data object to resolve (can be JsonElement, typed object, or null).</param>
    /// <param name="schema">Optional schema for validation. When null, no validation is performed.</param>
    /// <returns>Typed model instance, or null if data parameter was null.</returns>
    TModel? ResolveModel<TModel>(object? data, AIEditableModelSchema? schema = null)
        where TModel : class, new();

    /// <summary>
    /// Resolves a model from storage format to an instance of the specified runtime type.
    /// This non-generic overload is used when the target type is only known at runtime, such as
    /// grader, guardrail evaluator, and test feature configuration types.
    /// Supports configuration variable substitution using the pattern: $ConfigKey
    /// Validates the resolved model using the schema's validation rules when a schema is provided.
    /// </summary>
    /// <param name="modelType">The type of model to resolve to.</param>
    /// <param name="data">The data object to resolve (can be JsonElement, typed object, or null).</param>
    /// <param name="schema">Optional schema for validation. When null, no validation is performed.</param>
    /// <returns>Resolved model instance, or null if data parameter was null.</returns>
    object? ResolveModel(Type modelType, object? data, AIEditableModelSchema? schema = null)
    {
        // Default implementation delegates to the generic overload via reflection so that
        // existing custom implementations of this interface remain backwards compatible.
        var genericMethod = typeof(IAIEditableModelResolver)
            .GetMethods()
            .Single(m => m.Name == nameof(ResolveModel) && m.IsGenericMethodDefinition)
            .MakeGenericMethod(modelType);

        return genericMethod.Invoke(this, new[] { data, schema });
    }

    /// <summary>
    /// Escapes a literal value so it survives configuration-variable resolution unchanged.
    /// A value that already starts with the <c>$</c> reference prefix is prefixed with an extra
    /// <c>$</c> (e.g. <c>$5</c> becomes <c>$$5</c>), which <see cref="ResolveModel{TModel}"/>
    /// un-escapes back to the original. This is the inverse of the <c>$$</c> un-escape step and is
    /// lossless. Values that do not start with <c>$</c> are returned unchanged.
    /// </summary>
    /// <param name="value">The literal value to escape (may be null).</param>
    /// <returns>The escaped value, safe to store as settings that resolve back to the original literal.</returns>
    string? EscapeLiteral(string? value)
        => value is not null && value.StartsWith("$", StringComparison.Ordinal) ? "$" + value : value;
}
