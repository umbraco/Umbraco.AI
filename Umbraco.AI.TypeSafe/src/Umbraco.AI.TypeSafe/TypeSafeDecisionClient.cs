#pragma warning disable UMBRACOAI_DECISION // Implements the experimental IAIDecisionClient contract

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Decision;

namespace Umbraco.AI.TypeSafe;

/// <summary>
/// Decision client for TypeSafe AI (Jev). Sends every question in an <see cref="AIDecisionRequest"/>,
/// keyed by its own id, to a single <c>POST {Endpoint}/v1/systemone</c> call and maps the response back
/// to a keyed <see cref="AIDecisionResponse"/>.
/// </summary>
/// <remarks>
/// Wire shape confirmed against a live key and <c>docs.typesafe.ai/api</c> — see
/// <c>docs/archive/decision-capability/DECISION-LOG.md</c> ("T11") and
/// <c>docs/archive/decision-capability-release/SPEC.md</c> ("Provider: Umbraco.AI.TypeSafe"). Jev documents
/// no maximum number of questions per request (only per-question limits — 255 choice options, 10 score
/// levels — already enforced by Core's <c>DecisionQuestionValidator</c>), so this client enforces none.
/// </remarks>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public sealed class TypeSafeDecisionClient : IAIDecisionClient
{
    private const string DefaultModel = "jev-latest";
    private const string SystemOnePath = "/v1/systemone";

    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(30);
    private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly TypeSafeProviderSettings _settings;
    private readonly string? _modelId;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;
    private readonly Func<DateTimeOffset> _now;

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
    /// <param name="now">
    /// The clock read when computing the wait for an HTTP-date <c>Retry-After</c> header. Injected so retry
    /// tests can pin "now" instead of racing a real clock; production code defaults to
    /// <see cref="DateTimeOffset.UtcNow"/>.
    /// </param>
    internal TypeSafeDecisionClient(
        HttpClient httpClient,
        TypeSafeProviderSettings settings,
        string? modelId,
        Func<TimeSpan, CancellationToken, Task> delay,
        Func<DateTimeOffset>? now = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _modelId = modelId;
        _delay = delay ?? throw new ArgumentNullException(nameof(delay));
        _now = now ?? (() => DateTimeOffset.UtcNow);
    }

    /// <inheritdoc />
    public async Task<AIDecisionResponse> GetResponseAsync(
        AIDecisionRequest request,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestJson = JsonSerializer.Serialize(BuildRequest(request, options), SerializerOptions);

        using var response = await SendWithRetryAsync(requestJson, BuildRequestUri(), cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        var parsed = JsonSerializer.Deserialize<SystemOneResponse>(responseJson, SerializerOptions)
            ?? throw new JsonException("TypeSafe AI returned an empty response body.");

        if (parsed.Answers is null)
        {
            throw new JsonException("TypeSafe AI response did not include any answers.");
        }

        var usage = parsed.Usage is null
            ? null
            : new UsageDetails
            {
                InputTokenCount = parsed.Usage.InputTokens,
                OutputTokenCount = parsed.Usage.OutputTokens,
                TotalTokenCount = parsed.Usage.InputTokens + parsed.Usage.OutputTokens,
            };

        // Map every answer Jev returned — including one keyed by an id we never asked about — rather than
        // only the ones matching a question. Dropping an unasked-for answer here would hide it from
        // DecisionAnswerChecker, whose job (not this adapter's) is to reject it as an extra; see
        // ARCHITECTURE.md's "Checks".
        var questionsById = request.Questions.ToDictionary(q => q.Id!);

        var answers = new Dictionary<string, AIDecisionAnswer>();
        foreach (var (id, answerElement) in parsed.Answers)
        {
            answers[id] = questionsById.TryGetValue(id, out var question)
                ? MapAnswer(question, answerElement)
                : MapUnaskedAnswer(answerElement);
        }

        foreach (var question in request.Questions)
        {
            if (!answers.ContainsKey(question.Id!))
            {
                throw new JsonException($"TypeSafe AI response did not include an answer for question '{question.Id}'.");
            }
        }

        return new AIDecisionResponse
        {
            Answers = answers,
            ModelId = parsed.Model,
            Usage = usage,
            RawRepresentation = parsed,
        };
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

    private Uri BuildRequestUri()
    {
        var endpoint = string.IsNullOrWhiteSpace(_settings.Endpoint)
            ? "https://api.typesafe.ai"
            : _settings.Endpoint.TrimEnd('/');

        return new Uri($"{endpoint}{SystemOnePath}");
    }

    /// <summary>
    /// Builds the request body. <c>state</c> is <see cref="AIDecisionRequest.State"/> when set, or the
    /// first question's <see cref="AIDecisionQuestion.Instructions"/> otherwise — Jev requires a state,
    /// but <see cref="AIDecisionRequest.State"/> is optional at the Core level.
    /// </summary>
    private SystemOneRequest BuildRequest(AIDecisionRequest request, AIDecisionOptions? options) => new()
    {
        State = request.State ?? request.Questions[0].Instructions,
        Model = options?.ModelId ?? _modelId ?? DefaultModel,
        Questions = request.Questions.ToDictionary(q => q.Id!, BuildQuestion),
    };

    private static SystemOneQuestion BuildQuestion(AIDecisionQuestion question) => question switch
    {
        AIBinaryDecisionQuestion binary => new SystemOneQuestion
        {
            Type = "noul",
            Instructions = binary.Instructions,
            Criteria = BuildBinaryCriteria(binary),
        },
        AIChoiceDecisionQuestion choice => new SystemOneQuestion
        {
            Type = "choice",
            Instructions = choice.Instructions,
            Criteria = BuildChoiceCriteria(choice.Options),
        },
        AIScoreDecisionQuestion score => new SystemOneQuestion
        {
            Type = "score",
            Instructions = score.Instructions,
            Criteria = score.Levels.Select(l => l.Description).ToArray(),
        },
        _ => throw new NotSupportedException($"Unsupported decision question type '{question.GetType().Name}'."),
    };

    /// <summary>
    /// Builds the <c>criteria</c> object for a <c>noul</c> question. Jev's validator rejects an explicit
    /// <c>"criteria": null</c>, so the whole property is omitted (returns <see langword="null"/>) unless
    /// either <see cref="AIBinaryDecisionQuestion.TrueCriteria"/> or <see cref="AIBinaryDecisionQuestion.FalseCriteria"/>
    /// is actually set.
    /// </summary>
    private static Dictionary<string, string>? BuildBinaryCriteria(AIBinaryDecisionQuestion question)
    {
        if (question.TrueCriteria is null && question.FalseCriteria is null)
        {
            return null;
        }

        var criteria = new Dictionary<string, string>();
        if (question.TrueCriteria is not null)
        {
            criteria["true"] = question.TrueCriteria;
        }

        if (question.FalseCriteria is not null)
        {
            criteria["false"] = question.FalseCriteria;
        }

        return criteria;
    }

    /// <summary>
    /// Builds the <c>criteria</c> object for a <c>choice</c> question as a <see cref="JsonObject"/> rather
    /// than a <see cref="Dictionary{TKey,TValue}"/>, to guarantee the options are serialized in the same
    /// order they were declared in.
    /// </summary>
    private static JsonObject BuildChoiceCriteria(IReadOnlyList<AIDecisionOption> options)
    {
        var criteria = new JsonObject();
        foreach (var option in options)
        {
            criteria[option.Key] = option.Description ?? option.Key;
        }

        return criteria;
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(string requestJson, Uri requestUri, CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            var canRetry = attempt < maxAttempts && IsRetryableStatus(response.StatusCode);
            if (!canRetry)
            {
                var statusCode = response.StatusCode;
                string body;
                try
                {
                    body = await response.Content.ReadAsStringAsync(cancellationToken);
                }
                finally
                {
                    response.Dispose();
                }

                throw new HttpRequestException(
                    $"TypeSafe AI returned {(int)statusCode} ({statusCode}). {body}",
                    inner: null,
                    statusCode: statusCode);
            }

            var delay = GetRetryDelay(response, attempt);
            response.Dispose();
            await _delay(delay, cancellationToken);
        }

        // Unreachable: every iteration either returns a success response or throws.
        throw new InvalidOperationException("TypeSafe AI retry loop exited without a response.");
    }

    /// <summary>429 (rate limited) and 529 (Jev's overload status) are the only retried statuses.</summary>
    private static bool IsRetryableStatus(HttpStatusCode statusCode)
        => statusCode == HttpStatusCode.TooManyRequests || (int)statusCode == 529;

    /// <summary>
    /// Honors <c>Retry-After</c> (seconds or an HTTP-date), falling back to exponential backoff when it's
    /// absent. Either source is capped at <see cref="MaxRetryDelay"/> so a hostile or buggy header can't
    /// stall a request indefinitely.
    /// </summary>
    private TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter?.Delta is { } delta)
        {
            return Clamp(delta);
        }

        if (retryAfter?.Date is { } date)
        {
            var wait = date - _now();
            return Clamp(wait > TimeSpan.Zero ? wait : TimeSpan.Zero);
        }

        return Clamp(TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)));
    }

    private static TimeSpan Clamp(TimeSpan delay)
        => delay > MaxRetryDelay ? MaxRetryDelay : delay < TimeSpan.Zero ? TimeSpan.Zero : delay;

    private static AIDecisionAnswer MapAnswer(AIDecisionQuestion question, JsonElement answer) => question switch
    {
        AIBinaryDecisionQuestion => new AIBinaryDecisionAnswer
        {
            TrueProbability = GetRequiredDouble(answer, "noul"),
            RawRepresentation = answer,
        },
        AIChoiceDecisionQuestion choice => new AIChoiceDecisionAnswer
        {
            Choice = GetRequiredString(answer, "choice"),
            Confidence = GetOptionalDouble(answer, "confidence"),
            Probabilities = BuildChoiceProbabilities(choice, answer),
            RawRepresentation = answer,
        },
        AIScoreDecisionQuestion score => new AIScoreDecisionAnswer
        {
            Score = GetRequiredDouble(answer, "score"),
            Confidence = GetOptionalDouble(answer, "confidence"),
            Probabilities = BuildScoreProbabilities(score, answer),
            RawRepresentation = answer,
        },
        _ => throw new NotSupportedException($"Unsupported decision question type '{question.GetType().Name}'."),
    };

    /// <summary>
    /// Maps an answer keyed by an id Jev returned that wasn't among the questions we asked, using the
    /// answer's own <c>type</c> field — Jev's wire format always includes one (see docs.typesafe.ai/api,
    /// "Answer types") — rather than a question's expected shape, since there is no matching question to
    /// consult. Mapping it, rather than dropping it, is what lets <c>DecisionAnswerChecker</c> (Core) see
    /// and reject it as an answer to a question that was never asked; see ARCHITECTURE.md's "Checks".
    /// With no question to validate against, probabilities pass through exactly as Jev reported them — no
    /// gap-filling, no dropping.
    /// </summary>
    private static AIDecisionAnswer MapUnaskedAnswer(JsonElement answer)
    {
        var type = GetRequiredString(answer, "type");
        return type switch
        {
            "noul" => new AIBinaryDecisionAnswer
            {
                TrueProbability = GetRequiredDouble(answer, "noul"),
                RawRepresentation = answer,
            },
            "choice" => new AIChoiceDecisionAnswer
            {
                Choice = GetRequiredString(answer, "choice"),
                Confidence = GetOptionalDouble(answer, "confidence"),
                Probabilities = GetStringKeyedDoubles(answer, "probabilities"),
                RawRepresentation = answer,
            },
            "score" => new AIScoreDecisionAnswer
            {
                Score = GetRequiredDouble(answer, "score"),
                Confidence = GetOptionalDouble(answer, "confidence"),
                Probabilities = GetIntKeyedDoubles(answer, "probabilities"),
                RawRepresentation = answer,
            },
            _ => throw new JsonException($"TypeSafe AI answered with an unrecognized type '{type}'."),
        };
    }

    /// <summary>
    /// Builds a <c>choice</c> answer's probability distribution: every key Jev reported, kept as-is
    /// (including one outside the question's options — a contract violation <see cref="DecisionAnswerChecker"/>,
    /// not this adapter, rejects), plus a 0 for every option key Jev omitted — Jev only reports non-zero
    /// entries, and the checker requires the distribution to cover every option.
    /// </summary>
    private static Dictionary<string, double> BuildChoiceProbabilities(AIChoiceDecisionQuestion question, JsonElement answer)
    {
        var probabilities = GetStringKeyedDoubles(answer, "probabilities");

        foreach (var option in question.Options)
        {
            if (!probabilities.ContainsKey(option.Key))
            {
                probabilities[option.Key] = 0;
            }
        }

        return probabilities;
    }

    /// <summary>
    /// Builds a <c>score</c> answer's probability distribution, keyed by 0-based level index: every index
    /// Jev reported, kept as-is (including one outside <c>[0, Levels.Count - 1]</c> — a contract violation
    /// <see cref="DecisionAnswerChecker"/>, not this adapter, rejects), plus a 0 for every index 0..N-1 Jev
    /// omitted — Jev only reports non-zero entries, and the checker requires the distribution to cover
    /// every level.
    /// </summary>
    private static Dictionary<int, double> BuildScoreProbabilities(AIScoreDecisionQuestion question, JsonElement answer)
    {
        var probabilities = GetIntKeyedDoubles(answer, "probabilities");

        for (var index = 0; index < question.Levels.Count; index++)
        {
            if (!probabilities.ContainsKey(index))
            {
                probabilities[index] = 0;
            }
        }

        return probabilities;
    }

    private static double GetRequiredDouble(JsonElement answer, string propertyName)
        => answer.TryGetProperty(propertyName, out var value)
            ? value.GetDouble()
            : throw new JsonException($"TypeSafe AI answer is missing the required '{propertyName}' field.");

    private static double? GetOptionalDouble(JsonElement answer, string propertyName)
        => answer.TryGetProperty(propertyName, out var value) ? value.GetDouble() : null;

    private static string GetRequiredString(JsonElement answer, string propertyName)
        => answer.TryGetProperty(propertyName, out var value) && value.GetString() is { } str
            ? str
            : throw new JsonException($"TypeSafe AI answer is missing the required '{propertyName}' field.");

    private static Dictionary<string, double> GetStringKeyedDoubles(JsonElement answer, string propertyName)
    {
        if (!answer.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException($"TypeSafe AI answer is missing the required '{propertyName}' field.");
        }

        var result = new Dictionary<string, double>();
        foreach (var property in element.EnumerateObject())
        {
            result[property.Name] = property.Value.GetDouble();
        }

        return result;
    }

    /// <summary>
    /// Reads <paramref name="propertyName"/> as a map keyed by 0-based level index. A key that isn't an
    /// integer is a malformed response, not a level we can silently ignore, so it throws rather than being
    /// skipped.
    /// </summary>
    private static Dictionary<int, double> GetIntKeyedDoubles(JsonElement answer, string propertyName)
    {
        var result = new Dictionary<int, double>();
        foreach (var (key, value) in GetStringKeyedDoubles(answer, propertyName))
        {
            if (!int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index))
            {
                throw new JsonException($"TypeSafe AI answer's '{propertyName}' contains a non-integer key '{key}'.");
            }

            result[index] = value;
        }

        return result;
    }

    private sealed class SystemOneRequest
    {
        [JsonPropertyName("state")]
        public required string State { get; init; }

        [JsonPropertyName("model")]
        public required string Model { get; init; }

        [JsonPropertyName("questions")]
        public required Dictionary<string, SystemOneQuestion> Questions { get; init; }
    }

    private sealed class SystemOneQuestion
    {
        [JsonPropertyName("type")]
        public required string Type { get; init; }

        [JsonPropertyName("instructions")]
        public required string Instructions { get; init; }

        /// <summary>
        /// Left <see langword="null"/> to omit the property entirely — Jev rejects an explicit
        /// <c>"criteria": null</c> for a <c>noul</c> question.
        /// </summary>
        [JsonPropertyName("criteria")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Criteria { get; init; }
    }

    private sealed class SystemOneResponse
    {
        [JsonPropertyName("model")]
        public string? Model { get; init; }

        [JsonPropertyName("answers")]
        public Dictionary<string, JsonElement>? Answers { get; init; }

        [JsonPropertyName("usage")]
        public SystemOneUsage? Usage { get; init; }
    }

    private sealed class SystemOneUsage
    {
        [JsonPropertyName("input_tokens")]
        public long InputTokens { get; init; }

        [JsonPropertyName("output_tokens")]
        public long OutputTokens { get; init; }
    }
}
