using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Umbraco.AI.Core.Chat;

/// <summary>
/// Represents a schema that constrains AI output to a specific structure.
/// Can be created from a compile-time type or a runtime JSON Schema document.
/// </summary>
/// <remarks>
/// <para>
/// Use this type to define structured output requirements for chat and agent services.
/// Both typed and schema-driven approaches produce a <see cref="ChatResponseFormat"/>
/// that instructs the AI provider to return JSON conforming to the schema.
/// </para>
/// <para>
/// <strong>Typed (developer API):</strong>
/// </para>
/// <code>
/// var schema = AIOutputSchema.FromType&lt;MyResponse&gt;();
/// </code>
/// <para>
/// <strong>Schema-driven (UI/automation):</strong>
/// </para>
/// <code>
/// var schema = AIOutputSchema.FromJsonSchema(jsonElement);
/// </code>
/// </remarks>
public sealed class AIOutputSchema
{
    /// <summary>
    /// Gets the <see cref="ChatResponseFormat"/> that applies this schema constraint.
    /// </summary>
    public ChatResponseFormat ResponseFormat { get; }

    private AIOutputSchema(ChatResponseFormat responseFormat)
        => ResponseFormat = responseFormat;

    /// <summary>
    /// Creates an output schema from a compile-time type.
    /// The JSON Schema is derived automatically from the type's structure.
    /// </summary>
    /// <typeparam name="T">The type to generate a schema for.</typeparam>
    /// <returns>An <see cref="AIOutputSchema"/> that constrains output to match <typeparamref name="T"/>.</returns>
    public static AIOutputSchema FromType<T>()
        => new(ChatResponseFormat.ForJsonSchema<T>());

    /// <summary>
    /// Creates an output schema from a runtime <see cref="Type"/>.
    /// The JSON Schema is derived automatically from the type's structure.
    /// </summary>
    /// <param name="type">The type to generate a schema for.</param>
    /// <returns>An <see cref="AIOutputSchema"/> that constrains output to match <paramref name="type"/>.</returns>
    public static AIOutputSchema FromType(Type type)
        => new(ChatResponseFormat.ForJsonSchema(type));

    /// <summary>
    /// Creates an output schema from a raw JSON Schema document.
    /// Use this for schemas defined at runtime (e.g., in the backoffice UI or automation workflows).
    /// </summary>
    /// <param name="schema">A JSON Schema document as a <see cref="JsonElement"/>.</param>
    /// <returns>An <see cref="AIOutputSchema"/> that constrains output to the provided schema.</returns>
    public static AIOutputSchema FromJsonSchema(JsonElement schema)
        => new(ChatResponseFormat.ForJsonSchema(schema));
}
