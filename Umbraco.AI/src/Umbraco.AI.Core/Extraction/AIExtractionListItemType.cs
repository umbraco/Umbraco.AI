using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Extraction;

/// <summary>The kind of item an <see cref="AIListExtractionField"/> collects.</summary>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public enum AIExtractionListItemType
{
    /// <summary>Free text items.</summary>
    Text,

    /// <summary>Numeric items.</summary>
    Number,

    /// <summary>Date items.</summary>
    Date
}
