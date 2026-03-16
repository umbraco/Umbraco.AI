namespace Umbraco.AI.Core.Guardrails.Evaluators;

/// <summary>
/// A text span identified by an evaluator as a candidate for redaction.
/// </summary>
/// <param name="Index">The zero-based start position of the match in the content.</param>
/// <param name="Length">The length of the matched text.</param>
/// <param name="OriginalValue">The original matched text.</param>
public sealed record AIGuardrailRedactableMatch(int Index, int Length, string OriginalValue);
