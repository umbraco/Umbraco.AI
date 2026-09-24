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
/// The wire contract (endpoint, request/response shape) is confirmed against Jev's real API docs
/// (<see href="https://docs.typesafe.ai/api"/>) as of T11's live verification, 2026-09-24 — no
/// longer a guess. Jev's real endpoint answers a batch of named questions per call; this client
/// always sends exactly one, keyed <c>"q"</c>, to keep <see cref="IAIDecisionClient.AskAsync"/>'s
/// one-question-in-one-answer-out contract. <c>state</c> (the content being evaluated) and
/// <c>instructions</c> (the question about it) are two distinct fields in Jev's real API; this
/// spike has only one <see cref="AIDecisionQuestion.Prompt"/>, so it's sent as both — good enough
/// for a spike, but a real integration would want to separate the two.
/// </para>
/// <para>
/// Does <b>not</b> own the <see cref="HttpClient"/> passed to it, and must not dispose it.
/// <see cref="JevSpikeDecisionCapability"/> sources it from <see cref="JevSpikeProvider.CreateHttpClient"/>,
/// which is backed by <see cref="IHttpClientFactory"/> — the client itself doesn't own the pooled
/// handler, so disposing it here would be a no-op either way; the empty <see cref="Dispose"/> below
/// just makes that explicit rather than relying on a false "safe because it's shared" claim.
/// </para>
/// </remarks>
public sealed class JevSpikeDecisionClient(
    HttpClient httpClient,
    string apiKey,
    string answerPath = JevSpikeProviderSettings.DefaultAnswerPath) : IAIDecisionClient
{
    private const string QuestionKey = "q";

    /// <inheritdoc />
    public async Task<AIDecisionResponse> AskAsync(
        AIDecisionQuestion question,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var jevQuestion = new JevQuestionDto(
            Type: ToJevType(question.Kind),
            Instructions: question.Prompt,
            Criteria: BuildCriteria(question));

        var jevRequest = new JevSystemOneRequest(
            State: question.Prompt,
            Model: "jev-latest",
            Questions: new Dictionary<string, JevQuestionDto> { [QuestionKey] = jevQuestion });

        using var request = new HttpRequestMessage(HttpMethod.Post, answerPath)
        {
            Content = JsonContent.Create(jevRequest),
        };
        request.Headers.Add("Authorization", $"Bearer {apiKey}");

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException($"Jev returned {(int)response.StatusCode}: {errorBody}");
        }

        var body = await response.Content.ReadFromJsonAsync<JevSystemOneResponse>(cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new JsonException("Jev returned an empty response body.");

        if (!body.Answers.TryGetValue(QuestionKey, out var answer))
        {
            throw new JsonException($"Jev's response had no answer for question '{QuestionKey}'.");
        }

        return answer.Type switch
        {
            "noul" => MapNoul(answer, options?.ModelId),
            "choice" => AIDecisionResponse.ForChoice(
                answer.Choice ?? throw new JsonException("Jev returned a 'choice' answer with no selected choice."),
                answer.Confidence ?? throw new JsonException("Jev returned a 'choice' answer with no confidence."),
                options?.ModelId),
            "score" => AIDecisionResponse.ForScore(
                answer.Score ?? throw new JsonException("Jev returned a 'score' answer with no numeric value."),
                answer.Confidence ?? throw new JsonException("Jev returned a 'score' answer with no confidence."),
                options?.ModelId),
            _ => throw new JsonException($"Unrecognized Jev answer type '{answer.Type}'."),
        };
    }

    /// <summary>
    /// Jev's <c>noul</c> answer is itself a 0.0-1.0 probability of "yes", with no separate
    /// confidence field. Threshold at 0.5 for the yes/no answer, and report confidence as the
    /// probability of whichever side won — the further from 0.5, the more confident.
    /// </summary>
    private static AIDecisionResponse MapNoul(JevAnswerDto answer, string? modelId)
    {
        var noul = answer.Noul ?? throw new JsonException("Jev returned a 'noul' answer with no probability value.");
        var binaryAnswer = noul >= 0.5;
        var confidence = binaryAnswer ? noul : 1.0 - noul;
        return AIDecisionResponse.ForBinary(binaryAnswer, confidence, modelId);
    }

    /// <summary>
    /// Jev's type discriminator does not mirror <see cref="AIDecisionKind"/>'s own names — most
    /// notably, its yes/no type is called <c>noul</c>, not <c>binary</c>. An explicit mapping (rather
    /// than <c>ToString().ToLowerInvariant()</c>) is required so this stays correct even if either
    /// enum's member names ever change.
    /// </summary>
    private static string ToJevType(AIDecisionKind kind) => kind switch
    {
        AIDecisionKind.Binary => "noul",
        AIDecisionKind.Choice => "choice",
        AIDecisionKind.Score => "score",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unrecognized decision kind."),
    };

    /// <summary>
    /// Jev's <c>choice</c> questions require a description per option (<c>criteria</c>). This
    /// spike's <see cref="AIDecisionQuestion.Choices"/> carries only bare option strings, so each
    /// choice's own text stands in as its description — a real integration would want richer
    /// per-choice metadata on the question type.
    /// </summary>
    private static object? BuildCriteria(AIDecisionQuestion question)
        => question.Kind switch
        {
            AIDecisionKind.Choice => question.Choices!.ToDictionary(c => c, c => c),
            _ => null,
        };

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceType == typeof(IAIDecisionClient) ? this : null;

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
