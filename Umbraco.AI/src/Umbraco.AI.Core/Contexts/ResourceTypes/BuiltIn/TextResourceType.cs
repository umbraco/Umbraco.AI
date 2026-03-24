namespace Umbraco.AI.Core.Contexts.ResourceTypes.BuiltIn;

/// <summary>
/// Resource type for free-form text/markdown instructions.
/// </summary>
[AIContextResourceType("text", "Text",
    Description = "Free-form text or markdown instructions",
    Icon = "icon-document")]
public sealed class TextResourceType : AIContextResourceTypeBase<TextResourceSettings>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextResourceType"/> class.
    /// </summary>
    /// <param name="infrastructure">The infrastructure dependencies.</param>
    public TextResourceType(IAIContextResourceTypeInfrastructure infrastructure)
        : base(infrastructure)
    { }

    /// <inheritdoc />
    protected override string FormatDataForLlm(TextResourceSettings data)
    {
        return data.Content?.Trim() ?? string.Empty;
    }
}
