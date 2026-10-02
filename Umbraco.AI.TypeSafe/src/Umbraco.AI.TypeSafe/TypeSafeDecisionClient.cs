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
/// Decision client for TypeSafe AI (Jev). Sends one typed question per call to
/// <c>POST {Endpoint}/v1/systemone</c> and maps the response back to the matching
/// <see cref="AIDecisionResponse"/> subtype.
/// </summary>
/// <remarks>
/// Wire shape confirmed against a live key and <c>docs.typesafe.ai/api</c> — see
/// <c>docs/archive/decision-capability/DECISION-LOG.md</c> ("T11") and
/// <c>docs/plans/decision-capability-release/SPEC.md</c> ("Provider: Umbraco.AI.TypeSafe").
/// </remarks>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public sealed class TypeSafeDecisionClient : IAIDecisionClient
{
    private const string DefaultModel = "jev-latest";
    private const string QuestionKey = "q";
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
    public async Task<AIDecisionResponse> AskAsync(
        AIDecisionQuestion question,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(question);

        var requestJson = JsonSerializer.Serialize(BuildRequest(question, options), SerializerOptions);

        using var response = await SendWithRetryAsync(requestJson, BuildRequestUri(), cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        var parsed = JsonSerializer.Deserialize<SystemOneResponse>(responseJson, SerializerOptions)
            ?? throw new JsonException("TypeSafe AI returned an empty response body.");

        if (parsed.Answers is null || !parsed.Answers.TryGetValue(QuestionKey, out var answer))
        {
            throw new JsonException("TypeSafe AI response did not include an answer for the question.");
        }

        return MapResponse(question, parsed, answer);
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

    private SystemOneRequest BuildRequest(AIDecisionQuestion question, AIDecisionOptions? options) => new()
    {
        State = question.Context ?? question.Instructions,
        Model = options?.ModelId ?? _modelId ?? DefaultModel,
        Questions = new Dictionary<string, SystemOneQuestion> { [QuestionKey] = BuildQuestion(question) },
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
            Criteria = score.Levels,
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

    private static AIDecisionResponse MapResponse(AIDecisionQuestion question, SystemOneResponse response, JsonElement answer)
    {
        var usage = response.Usage is null
            ? null
            : new UsageDetails
            {
                InputTokenCount = response.Usage.InputTokens,
                OutputTokenCount = response.Usage.OutputTokens,
                TotalTokenCount = response.Usage.InputTokens + response.Usage.OutputTokens,
            };

        return question switch
        {
            AIBinaryDecisionQuestion => new AIBinaryDecisionResponse
            {
                Probability = GetRequiredDouble(answer, "noul"),
                ModelId = response.Model,
                Usage = usage,
                RawRepresentation = response,
            },
            AIChoiceDecisionQuestion choice => new AIChoiceDecisionResponse
            {
                Choice = GetValidatedChoice(choice, answer),
                ChoiceConfidence = GetRequiredDouble(answer, "confidence"),
                Probabilities = GetStringKeyedDoubles(answer, "probabilities"),
                ModelId = response.Model,
                Usage = usage,
                RawRepresentation = response,
            },
            AIScoreDecisionQuestion score => MapScoreResponse(score, answer, response, usage),
            _ => throw new NotSupportedException($"Unsupported decision question type '{question.GetType().Name}'."),
        };
    }

    /// <summary>
    /// Maps a <c>score</c> answer. Jev reports <c>probabilities</c> keyed by 0-based level index, and also
    /// echoes a <c>legend</c> mapping each index back to a label — but we sent <see cref="AIScoreDecisionQuestion.Levels"/>
    /// in that same order, so it (not the echoed legend) is the authoritative source for both
    /// <see cref="AIScoreDecisionResponse.Level"/> and the keys of <see cref="AIScoreDecisionResponse.Probabilities"/>.
    /// The response's <c>legend</c> is otherwise unused.
    /// </summary>
    /// <remarks>
    /// <see cref="AIScoreDecisionResponse.Level"/> is picked by rounding <c>score</c> to the nearest integer
    /// (away from zero on a .5 tie) and clamping it to <c>[0, Levels.Count - 1]</c>, so an out-of-range score
    /// (e.g. below 0 or above the last level) still resolves to the nearest end level rather than throwing.
    /// A <c>probabilities</c> key that falls outside that same <c>[0, Levels.Count - 1]</c> range — Jev
    /// reporting a level index we never sent — is silently dropped rather than surfaced or thrown.
    /// </remarks>
    private static AIScoreDecisionResponse MapScoreResponse(
        AIScoreDecisionQuestion question,
        JsonElement answer,
        SystemOneResponse response,
        UsageDetails? usage)
    {
        var score = GetRequiredDouble(answer, "score");
        var probabilitiesByIndex = GetStringKeyedDoubles(answer, "probabilities");

        var nearestIndex = Math.Clamp(
            (int)Math.Round(score, MidpointRounding.AwayFromZero),
            0,
            Math.Max(question.Levels.Count - 1, 0));

        var level = question.Levels[nearestIndex];

        var probabilities = new Dictionary<string, double>();
        foreach (var (index, probability) in probabilitiesByIndex)
        {
            if (int.TryParse(index, NumberStyles.Integer, CultureInfo.InvariantCulture, out var levelIndex)
                && levelIndex >= 0
                && levelIndex < question.Levels.Count)
            {
                probabilities[question.Levels[levelIndex]] = probability;
            }
        }

        return new AIScoreDecisionResponse
        {
            Score = score,
            Level = level,
            ScoreConfidence = GetRequiredDouble(answer, "confidence"),
            Probabilities = probabilities,
            ModelId = response.Model,
            Usage = usage,
            RawRepresentation = response,
        };
    }

    private static double GetRequiredDouble(JsonElement answer, string propertyName)
        => answer.TryGetProperty(propertyName, out var value)
            ? value.GetDouble()
            : throw new JsonException($"TypeSafe AI answer is missing the required '{propertyName}' field.");

    private static string GetRequiredString(JsonElement answer, string propertyName)
        => answer.TryGetProperty(propertyName, out var value) && value.GetString() is { } str
            ? str
            : throw new JsonException($"TypeSafe AI answer is missing the required '{propertyName}' field.");

    /// <summary>
    /// Validates that the <c>choice</c> Jev answered with is one of the option keys we sent — a contract
    /// violation otherwise, since Jev can only choose from the criteria it was given.
    /// </summary>
    private static string GetValidatedChoice(AIChoiceDecisionQuestion question, JsonElement answer)
    {
        var choice = GetRequiredString(answer, "choice");
        return question.Options.Any(option => option.Key == choice)
            ? choice
            : throw new JsonException($"TypeSafe AI answer chose '{choice}', which is not one of the question's option keys.");
    }

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
