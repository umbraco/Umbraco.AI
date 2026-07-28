namespace Umbraco.AI.Anthropic;

/// <summary>
/// The per-model capability facts Umbraco.AI consumes from Anthropic's models endpoint.
/// </summary>
/// <param name="Id">The model ID.</param>
/// <param name="SupportsEffort">
/// Whether the model accepts <c>output_config.effort</c>, or <c>null</c> when the API did not report it —
/// in which case the caller infers support from the model ID instead of assuming either way.
/// </param>
internal sealed record AnthropicModelCapability(string Id, bool? SupportsEffort);
