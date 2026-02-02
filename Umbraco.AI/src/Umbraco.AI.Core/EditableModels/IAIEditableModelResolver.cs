using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Core.EditableModels;

/// <summary>
/// Service for resolving editable models from various storage formats.
/// Handles JSON deserialization, configuration variable substitution, and validation.
/// </summary>
public interface IAIEditableModelResolver
{
    /// <summary>
    /// Resolves a model from storage format (JsonElement, Dictionary, etc.) to a typed instance.
    /// Supports configuration variable substitution using the pattern: $ConfigKey
    /// Validates the resolved model using the schema's validation rules.
    /// </summary>
    /// <typeparam name="TModel">The type of model to resolve to.</typeparam>
    /// <param name="modelId">The model ID for validation context.</param>
    /// <param name="data">The data object to resolve (can be JsonElement, typed object, or null).</param>
    /// <returns>Typed model instance, or null if data parameter was null.</returns>
    TModel? ResolveModel<TModel>(string modelId, object? data)
        where TModel : class, new();
}
