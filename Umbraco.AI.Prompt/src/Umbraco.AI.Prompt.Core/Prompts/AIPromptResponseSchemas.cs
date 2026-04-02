namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Structured response schema for single-value prompts (OptionCount=1).
/// Used with <see cref="Microsoft.Extensions.AI.ChatResponseFormat.ForJsonSchema{T}"/>
/// to constrain the LLM to return only the content value without preamble.
/// </summary>
/// <remarks>
/// A wrapper object is necessary because many AI services require that the
/// JSON schema have a top-level 'type=object'. Primitive types like string may fail.
/// </remarks>
internal sealed record SingleValueResponse
{
    public required string Value { get; init; }
}

/// <summary>
/// Structured response schema for multi-option prompts (OptionCount>=2).
/// Used with <see cref="Microsoft.Extensions.AI.ChatResponseFormat.ForJsonSchema{T}"/>
/// to enforce the options JSON structure at the provider level.
/// </summary>
internal sealed record MultiOptionResponse
{
    public required IReadOnlyList<MultiOptionItem> Options { get; init; }
}

/// <summary>
/// A single option within a <see cref="MultiOptionResponse"/>.
/// </summary>
internal sealed record MultiOptionItem
{
    public required string Label { get; init; }

    public required string Value { get; init; }

    public string? Description { get; init; }
}
