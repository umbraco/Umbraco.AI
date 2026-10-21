using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Extraction;

/// <summary>
/// Where an extracted value sits in <see cref="AIExtractionRequest.State"/>, as character offsets.
/// </summary>
/// <param name="Start">The offset of the first character of the value.</param>
/// <param name="End">The offset just past the last character of the value (end-exclusive).</param>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public readonly record struct AIExtractionSpan(int Start, int End);
