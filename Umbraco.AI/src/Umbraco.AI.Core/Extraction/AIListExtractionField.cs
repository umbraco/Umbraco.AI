using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Extraction;

/// <summary>An <see cref="AIExtractionField"/> that extracts every matching item as a list.</summary>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public sealed class AIListExtractionField : AIExtractionField<AIListExtractionAnswer>
{
    /// <summary>The kind of item to collect. Sent to the provider; answers always come back as strings.</summary>
    public AIExtractionListItemType ItemType { get; init; } = AIExtractionListItemType.Text;
}
