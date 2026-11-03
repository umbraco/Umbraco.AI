using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Extraction;

/// <summary>An <see cref="AIExtractionField"/> that extracts a date and time.</summary>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public sealed class AIDateTimeExtractionField : AIExtractionField<AIDateTimeExtractionAnswer>;
