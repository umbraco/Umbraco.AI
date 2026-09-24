#pragma warning disable UMBRACOAI_DECISION // Implements the experimental IAIDecisionClient contract

using System.Diagnostics.CodeAnalysis;
using Umbraco.AI.Core.Decision;

namespace Umbraco.AI.TypeSafe;

/// <summary>
/// Decision client for TypeSafe AI (Jev). Sends one typed question per call to
/// <c>POST {Endpoint}/v1/systemone</c> and maps the response back to the matching
/// <see cref="AIDecisionResponse"/> subtype.
/// </summary>
/// <remarks>
/// The wire mapping, retry policy, and response parsing are implemented in a later task (T7 in the
/// decision-capability-release plan) — see <c>ARCHITECTURE.md</c> decision 3. This constructor's shape is
/// fixed now because the T7 specs (including the retry tests, which need to fake the delay so they don't
/// sleep) are written against it.
/// </remarks>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public sealed class TypeSafeDecisionClient : IAIDecisionClient
{
    private readonly HttpClient _httpClient;
    private readonly TypeSafeProviderSettings _settings;
    private readonly string? _modelId;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeDecisionClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to call the TypeSafe AI API with.</param>
    /// <param name="settings">The resolved provider settings (API key, endpoint).</param>
    /// <param name="modelId">The model to send with each request.</param>
    /// <param name="delay">
    /// The delay awaited between retry attempts. Injected so retry tests can fake it instead of actually
    /// sleeping; production code passes a real <see cref="Task.Delay(TimeSpan, CancellationToken)"/>.
    /// </param>
    internal TypeSafeDecisionClient(
        HttpClient httpClient,
        TypeSafeProviderSettings settings,
        string? modelId,
        Func<TimeSpan, CancellationToken, Task> delay)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _modelId = modelId;
        _delay = delay ?? throw new ArgumentNullException(nameof(delay));
    }

    /// <inheritdoc />
    public Task<AIDecisionResponse> AskAsync(
        AIDecisionQuestion question,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(question);

        // Wire mapping, response parsing, and the bounded 429/529 retry (using _delay) land in T7.
        throw new NotImplementedException(
            $"TypeSafeDecisionClient.AskAsync is implemented in a later task (model "
            + $"'{options?.ModelId ?? _modelId ?? "jev-latest"}', endpoint '{_settings.Endpoint}', "
            + $"retry delay configured: {_delay is not null}).");
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceKey is null && serviceType == typeof(HttpClient) ? _httpClient : null;

    /// <inheritdoc />
    public void Dispose()
    {
        // The HttpClient came from IHttpClientFactory; its handler is pooled/disposed by the factory,
        // not by this client, matching the rest of this codebase's provider clients.
    }
}
