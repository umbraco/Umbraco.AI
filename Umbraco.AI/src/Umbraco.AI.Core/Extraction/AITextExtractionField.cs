using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Extraction;

/// <summary>An <see cref="AIExtractionField"/> that extracts a piece of text.</summary>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public sealed class AITextExtractionField : AIExtractionField<AITextExtractionAnswer>;
