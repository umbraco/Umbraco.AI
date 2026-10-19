using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Decision;

/// <summary>One labelled level of an <see cref="AIScoreDecisionQuestion"/>, lowest first.</summary>
/// <param name="Description">The level's label, e.g. <c>"poor"</c>, <c>"ok"</c>, <c>"good"</c>.</param>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public sealed record AIDecisionScoreLevel(string Description);
