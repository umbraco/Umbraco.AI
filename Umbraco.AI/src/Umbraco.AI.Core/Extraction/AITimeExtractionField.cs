using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Extraction;

/// <summary>An <see cref="AIExtractionField"/> that extracts a time of day.</summary>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public sealed class AITimeExtractionField : AIExtractionField<AITimeExtractionAnswer>;
