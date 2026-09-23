#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability

using System.Net.Http.Json;
using System.Text.Json;
using Umbraco.AI.Core.Decision;

namespace Umbraco.AI.Tests.Common.Decision.Spike;

/// <summary>
/// The disposable HTTP-backed <see cref="IAIDecisionClient"/> for the Jev spike (decision-capability
/// plan T10) — calls Jev's API directly via <see cref="HttpClient"/> + <c>System.Text.Json</c>, no
/// reference to the community <c>RavenValentin/TypeSafe.Jev</c> SDK, per the plan's
/// <c>ARCHITECTURE.md</c> build-vs-buy decision.
/// </summary>
/// <remarks>
/// <para>
/// The request path and Jev's JSON field names are placeholders pending T11 confirming Jev's actual
/// wire contract from its real API docs against a live key — nobody has verified them yet. See
/// <see cref="JevSpikeProvider"/>'s remarks for why this whole spike lives outside <c>src/</c>.
/// </para>
/// <para>
/// Does <b>not</b> own the <see cref="HttpClient"/> passed to it, and must not dispose it.
/// <see cref="JevSpikeDecisionCapability"/> sources it from <see cref="JevSpikeProvider.CreateHttpClient"/>,
/// which is backed by <see cref="IHttpClientFactory"/> — the factory pools and recycles the underlying
/// handler, so disposing the <see cref="HttpClient"/> instance here would tear down a connection pool
/// shared with other callers, not a resource this client owns.
/// </para>
/// </remarks>
public sealed class JevSpikeDecisionClient(HttpClient httpClient, string apiKey, string answerPath = "/v1/answer") : IAIDecisionClient
{
    /// <inheritdoc />
    public async Task<AIDecisionResponse> AskAsync(
        AIDecisionQuestion question,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, answerPath)
        {
            Content = JsonContent.Create(new
            {
                prompt = question.Prompt,
                kind = question.Kind.ToString().ToLowerInvariant(),
                choices = question.Choices,
                scoreMin = question.ScoreRange?.Min,
                scoreMax = question.ScoreRange?.Max,
            }),
        };
        request.Headers.Add("Authorization", $"Bearer {apiKey}");

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var dto = await response.Content.ReadFromJsonAsync<JevAnswerDto>(cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new JsonException("Jev returned an empty response body.");

        return dto.Kind switch
        {
            "noul" => AIDecisionResponse.ForBinary(
                dto.Noul ?? throw new JsonException("Jev returned a 'noul' answer with no boolean value."),
                dto.Confidence,
                options?.ModelId),
            "choice" => AIDecisionResponse.ForChoice(
                dto.Choice ?? throw new JsonException("Jev returned a 'choice' answer with no selected choice."),
                dto.Confidence,
                options?.ModelId),
            "score" => AIDecisionResponse.ForScore(
                dto.Score ?? throw new JsonException("Jev returned a 'score' answer with no numeric value."),
                dto.Confidence,
                options?.ModelId),
            _ => throw new JsonException($"Unrecognized Jev answer kind '{dto.Kind}'."),
        };
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceType == typeof(IAIDecisionClient) ? this : null;

    /// <inheritdoc />
    /// <remarks>
    /// Intentionally a no-op: <c>httpClient</c> is factory-managed (see this class's remarks) and
    /// outlives any single <see cref="IAIDecisionClient"/> built around it.
    /// </remarks>
    public void Dispose()
    {
    }
}
