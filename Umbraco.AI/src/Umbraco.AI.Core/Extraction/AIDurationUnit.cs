using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Extraction;

/// <summary>The unit an <see cref="AIDurationExtractionField"/> is measured in.</summary>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public enum AIDurationUnit
{
    /// <summary>Seconds.</summary>
    Seconds,

    /// <summary>Minutes.</summary>
    Minutes,

    /// <summary>Hours.</summary>
    Hours,

    /// <summary>Days.</summary>
    Days,

    /// <summary>Weeks.</summary>
    Weeks,

    /// <summary>Months.</summary>
    Months,

    /// <summary>Years.</summary>
    Years
}
