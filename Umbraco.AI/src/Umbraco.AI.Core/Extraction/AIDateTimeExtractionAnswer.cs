using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Extraction;

/// <summary>The answer to an <see cref="AIDateTimeExtractionField"/>.</summary>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public sealed class AIDateTimeExtractionAnswer : AIExtractionAnswer
{
    /// <summary>
    /// The extracted value, or <see langword="null"/> when it isn't in the text. Providers set
    /// <see cref="DateTimeKind.Unspecified"/> unless the text gives an offset or zone, matching how
    /// <see cref="DateTime.Parse(string)"/> reads an ISO string that carries none; no local-time or UTC
    /// conversion is applied.
    /// </summary>
    public DateTime? Value { get; init; }

    /// <inheritdoc />
    public override bool IsFound => Value is not null;
}
