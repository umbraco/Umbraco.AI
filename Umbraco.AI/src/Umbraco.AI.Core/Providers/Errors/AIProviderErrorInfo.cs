namespace Umbraco.AI.Core.Providers.Errors;

/// <summary>
/// Result of classifying a provider SDK exception.
/// </summary>
/// <param name="Category">The normalised category.</param>
/// <param name="UserMessage">A message safe to render in user-facing surfaces.</param>
/// <param name="ProviderCode">
/// The original provider-specific error code (e.g. <c>overloaded_error</c>) when available.
/// Surface this in telemetry, not in the UI.
/// </param>
/// <param name="RawMessage">
/// The original exception message. Kept for logs and diagnostics — must not be rendered to end users.
/// </param>
public sealed record AIProviderErrorInfo(
    AIProviderErrorCategory Category,
    string UserMessage,
    string? ProviderCode,
    string RawMessage);
