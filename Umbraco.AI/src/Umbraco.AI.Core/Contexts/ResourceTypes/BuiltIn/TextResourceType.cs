namespace Umbraco.AI.Core.Contexts.ResourceTypes.BuiltIn;

/// <summary>
/// Resource type for free-form text/markdown instructions.
/// </summary>
[AIContextResourceType("text", "Text")]
public sealed class TextResourceType : AIContextResourceTypeBase<TextResourceData>
{
    /// <inheritdoc />
    public override string? Description => "Free-form text or markdown instructions";

    /// <inheritdoc />
    public override string? Icon => "icon-document";

    /// <summary>
    /// Initializes a new instance of the <see cref="TextResourceType"/> class.
    /// </summary>
    /// <param name="infrastructure">The infrastructure dependencies.</param>
    public TextResourceType(IAiContextResourceTypeInfrastructure infrastructure)
        : base(infrastructure)
    { }

    /// <inheritdoc />
    protected override string FormatForLlm(TextResourceData data)
    {
        return data.Content?.Trim() ?? string.Empty;
    }
}
