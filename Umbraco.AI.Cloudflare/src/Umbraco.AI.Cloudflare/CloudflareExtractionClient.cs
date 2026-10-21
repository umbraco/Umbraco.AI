#pragma warning disable UMBRACOAI_EXTRACTION // Implements the experimental Umbraco.AI extraction client contract

using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Extraction;

namespace Umbraco.AI.Cloudflare;

/// <summary>
/// Extraction client for Cloudflare's Clairic model. Sends every field, keyed by its own id, to a single
/// <c>POST {Endpoint}/accounts/{AccountId}/ai/run/@cf/cloudflare/clairic</c> call and maps the answers back
/// to typed <see cref="AIExtractionAnswer"/>s.
/// </summary>
/// <remarks>
/// The Clairic REST shape follows the Workers AI docs; the real response (wrapped in Cloudflare's
/// <c>{ result, success, errors }</c> envelope or bare) is accepted either way. Rejecting a missing, extra or
/// wrong-kind answer is Core's job (<c>ExtractionAnswerChecker</c>), not this adapter's.
/// </remarks>
internal sealed partial class CloudflareExtractionClient : IAIExtractionClient
{
    private const string RunPath = "ai/run/@cf/cloudflare/clairic";
    private const int MaxAttempts = 3;

    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(30);

    private readonly HttpClient _httpClient;
    private readonly CloudflareProviderSettings _settings;
    private readonly string _modelId;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;
    private readonly Func<DateTimeOffset> _now;

    /// <summary>
    /// Initializes a new instance of the <see cref="CloudflareExtractionClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to call the Cloudflare API with.</param>
    /// <param name="settings">The resolved provider settings (account ID, token, endpoint).</param>
    /// <param name="modelId">The model reported back when the response doesn't name one.</param>
    /// <param name="delay">The delay awaited between retry attempts; injected so tests don't sleep.</param>
    /// <param name="now">The clock read for an HTTP-date <c>Retry-After</c>; injected so tests can pin it.</param>
    internal CloudflareExtractionClient(
        HttpClient httpClient,
        CloudflareProviderSettings settings,
        string modelId,
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
    public async Task<AIExtractionResponse> GetResponseAsync(
        AIExtractionRequest request,
        AIExtractionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestJson = BuildRequest(request).ToJsonString();
        var requestUri = CloudflareApi.BuildAccountUri(_settings, RunPath);

        using var response = await SendWithRetryAsync(requestJson, requestUri, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        return ParseResponse(request, responseJson);
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;

    /// <inheritdoc />
    public void Dispose()
    {
        // The HttpClient came from IHttpClientFactory; its handler is pooled/disposed by the factory,
        // not by this client, matching the other provider clients.
    }

    private static JsonObject BuildRequest(AIExtractionRequest request)
    {
        var fields = new JsonObject();
        foreach (var field in request.Fields)
        {
            fields[field.Id!] = BuildField(field);
        }

        var body = new JsonObject();
        SetIfNotNull(body, "state", request.State);
        body["fields"] = fields;
        if (request.ReferenceDate is { } referenceDate)
        {
            body["reference_date"] = referenceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        return body;
    }

    private static JsonObject BuildField(AIExtractionField field)
    {
        var node = new JsonObject { ["type"] = TypeName(field), ["instructions"] = field.Instructions };

        switch (field)
        {
            case AINumberExtractionField number:
                SetNumberOptions(node, number.Decimals, number.Integer);
                SetIfNotNull(node, "unit", number.Unit);
                break;
            case AIDurationExtractionField duration:
                SetNumberOptions(node, duration.Decimals, duration.Integer);
                node["unit"] = DurationUnitName(duration.Unit);
                break;
            case AIListExtractionField list:
                node["items"] = ListItemTypeName(list.ItemType);
                break;
        }

        return node;
    }

    private static void SetNumberOptions(JsonObject node, int? decimals, bool integer)
    {
        if (decimals is { } places)
        {
            node["decimals"] = places;
        }

        if (integer)
        {
            node["integer"] = true;
        }
    }

    /// <summary>Cloudflare rejects an explicit null, so an unset value is left out of the body entirely.</summary>
    private static void SetIfNotNull(JsonObject node, string name, string? value)
    {
        if (value is not null)
        {
            node[name] = value;
        }
    }

    private static string TypeName(AIExtractionField field) => field switch
    {
        AITextExtractionField => "text",
        AINumberExtractionField => "number",
        AIDateExtractionField => "date",
        AITimeExtractionField => "time",
        AIDateTimeExtractionField => "datetime",
        AIDurationExtractionField => "duration",
        AIListExtractionField => "list",
        _ => throw new NotSupportedException($"Unsupported extraction field type '{field.GetType().Name}'."),
    };

    private static string DurationUnitName(AIDurationUnit unit) => unit.ToString().ToLowerInvariant();

    private static string ListItemTypeName(AIExtractionListItemType itemType) => itemType switch
    {
        AIExtractionListItemType.Text => "text",
        AIExtractionListItemType.Number => "number",
        AIExtractionListItemType.Date => "date",
        _ => throw new NotSupportedException($"Unsupported extraction list item type '{itemType}'."),
    };

    private async Task<HttpResponseMessage> SendWithRetryAsync(string requestJson, Uri requestUri, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            using var request = CloudflareApi.CreateRequest(HttpMethod.Post, requestUri, _settings, requestJson);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return response;
            }

            // Only a 429 is retried; every other failure, and a 429 on the last attempt, is reported.
            if (attempt == MaxAttempts || response.StatusCode != HttpStatusCode.TooManyRequests)
            {
                throw await CloudflareApi.CreateExceptionAsync(response, _settings, cancellationToken);
            }

            var delay = GetRetryDelay(response, attempt);
            response.Dispose();
            await _delay(delay, cancellationToken);
        }

        // Unreachable: every iteration either returns a success response or throws.
        throw new InvalidOperationException("Cloudflare retry loop exited without a response.");
    }

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
            return Clamp(date - _now());
        }

        return Clamp(TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)));
    }

    private static TimeSpan Clamp(TimeSpan delay)
        => delay > MaxRetryDelay ? MaxRetryDelay : delay < TimeSpan.Zero ? TimeSpan.Zero : delay;

    private AIExtractionResponse ParseResponse(AIExtractionRequest request, string responseJson)
    {
        using var document = JsonDocument.Parse(responseJson);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Cloudflare returned a response that isn't a JSON object.");
        }

        // Accept both the wrapped { result: {...} } and the bare { answers: {...} } shape.
        var payload = root.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.Object ? result : root;

        if (!payload.TryGetProperty("answers", out var answersElement) || answersElement.ValueKind != JsonValueKind.Object)
        {
            // Not an HttpRequestException: with no status it would be classified as a network failure, but
            // Cloudflare did answer.
            var errors = CloudflareApi.TryFormatErrors(root);
            throw errors is not null
                ? new InvalidOperationException($"Cloudflare reported a failure. {CloudflareApi.Describe(errors, _settings.ApiToken)}")
                : new JsonException("Cloudflare response did not include any answers.");
        }

        var fieldsById = request.Fields.ToDictionary(f => f.Id!);
        var answers = new Dictionary<string, AIExtractionAnswer>();

        // Map every answer returned, including one for an id nobody asked about, so Core's checker is the
        // one that rejects it rather than the adapter silently dropping it.
        foreach (var property in answersElement.EnumerateObject())
        {
            var answer = property.Value.Clone();
            answers[property.Name] = fieldsById.TryGetValue(property.Name, out var field)
                ? MapAnswer(field, answer)
                : MapUnaskedAnswer(answer);
        }

        return new AIExtractionResponse
        {
            Answers = answers,
            ModelId = payload.TryGetProperty("model", out var model) && model.ValueKind == JsonValueKind.String
                ? model.GetString()
                : _modelId,
            Usage = MapUsage(payload),
            RawRepresentation = root.Clone(),
        };
    }

    private static UsageDetails? MapUsage(JsonElement payload)
    {
        if (!payload.TryGetProperty("usage", out var usage) || usage.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var input = GetLong(usage, "input_tokens");
        var output = GetLong(usage, "output_tokens");
        return new UsageDetails
        {
            InputTokenCount = input,
            OutputTokenCount = output,
            TotalTokenCount = input + output,
        };
    }

    private static long GetLong(JsonElement element, string name)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetInt64() : 0;

    private static AIExtractionAnswer MapAnswer(AIExtractionField field, JsonElement answer)
    {
        var value = answer.TryGetProperty("value", out var v) ? v : default;
        var confidence = GetOptionalDouble(answer, "confidence");
        var nullProbability = GetOptionalDouble(answer, "p_null");

        return field switch
        {
            AITextExtractionField => new AITextExtractionAnswer
            {
                Value = ReadString(value),
                Span = IsNull(value) ? null : ReadSpan(answer, "span"),
                Confidence = confidence,
                NullProbability = nullProbability,
                RawRepresentation = answer,
            },
            AINumberExtractionField => new AINumberExtractionAnswer
            {
                Value = ReadDecimal(value),
                Confidence = confidence,
                NullProbability = nullProbability,
                RawRepresentation = answer,
            },
            AIDurationExtractionField duration => new AIDurationExtractionAnswer
            {
                Value = ReadDecimal(value),
                Unit = duration.Unit,
                Confidence = confidence,
                NullProbability = nullProbability,
                RawRepresentation = answer,
            },
            AIDateExtractionField => new AIDateExtractionAnswer
            {
                Value = ReadDate(value),
                Confidence = confidence,
                NullProbability = nullProbability,
                RawRepresentation = answer,
            },
            AITimeExtractionField => new AITimeExtractionAnswer
            {
                Value = ReadTime(value),
                Confidence = confidence,
                NullProbability = nullProbability,
                RawRepresentation = answer,
            },
            AIDateTimeExtractionField => new AIDateTimeExtractionAnswer
            {
                Value = ReadDateTime(value),
                Confidence = confidence,
                NullProbability = nullProbability,
                RawRepresentation = answer,
            },
            AIListExtractionField => new AIListExtractionAnswer
            {
                Values = ReadList(value),
                Spans = IsNull(value) ? [] : ReadSpans(answer, "spans"),
                Confidence = confidence,
                NullProbability = nullProbability,
                RawRepresentation = answer,
            },
            _ => throw new NotSupportedException($"Unsupported extraction field type '{field.GetType().Name}'."),
        };
    }

    /// <summary>
    /// Wraps an answer for an id that wasn't asked about. Nothing is parsed (there is no field to say what
    /// the value should be, and a malformed one mustn't throw), so Core's checker can see and reject it as
    /// an extra; the raw JSON is kept.
    /// </summary>
    private static AIExtractionAnswer MapUnaskedAnswer(JsonElement answer)
        => new AITextExtractionAnswer { RawRepresentation = answer };

    private static bool IsNull(JsonElement value)
        => value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null;

    private static double? GetOptionalDouble(JsonElement answer, string name)
        => answer.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetDouble() : null;

    private static string? ReadString(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Undefined or JsonValueKind.Null => null,
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number => NumberToString(value),
        _ => throw new JsonException($"Cloudflare answered a text field with a {value.ValueKind} value."),
    };

    private static decimal? ReadDecimal(JsonElement value)
    {
        if (IsNull(value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
        {
            return number;
        }

        if (value.ValueKind == JsonValueKind.String
            && decimal.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        throw new JsonException("Cloudflare answered a number field with a value that isn't a number.");
    }

    private static DateOnly? ReadDate(JsonElement value)
    {
        if (IsNull(value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String
            && DateOnly.TryParseExact(value.GetString(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : throw new JsonException("Cloudflare answered a date field with a value that isn't a yyyy-MM-dd date.");
    }

    private static TimeOnly? ReadTime(JsonElement value)
    {
        if (IsNull(value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String
            && TimeOnly.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)
            ? time
            : throw new JsonException("Cloudflare answered a time field with a value that isn't a time.");
    }

    /// <summary>
    /// Reads an ISO date-time. With a <c>Z</c> or offset it's returned as UTC; without one it stays
    /// <see cref="DateTimeKind.Unspecified"/>, with no local-time conversion.
    /// </summary>
    private static DateTime? ReadDateTime(JsonElement value)
    {
        if (IsNull(value))
        {
            return null;
        }

        var text = value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        if (text is not null && OffsetSuffix().IsMatch(text))
        {
            if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var withOffset))
            {
                return withOffset.UtcDateTime;
            }
        }
        else if (text is not null && DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var local))
        {
            return local;
        }

        throw new JsonException("Cloudflare answered a datetime field with a value that isn't an ISO date-time.");
    }

    private static IReadOnlyList<string> ReadList(JsonElement value)
    {
        if (IsNull(value))
        {
            return [];
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Cloudflare answered a list field with a value that isn't an array.");
        }

        return value.EnumerateArray().Select(item => ReadString(item) ?? string.Empty).ToList();
    }

    private static string NumberToString(JsonElement number)
        => number.TryGetDecimal(out var decimalValue)
            ? decimalValue.ToString(CultureInfo.InvariantCulture)
            : number.GetRawText();

    private static AIExtractionSpan? ReadSpan(JsonElement answer, string name)
        => answer.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array ? ToSpan(value) : null;

    private static IReadOnlyList<AIExtractionSpan> ReadSpans(JsonElement answer, string name)
    {
        if (!answer.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return value.EnumerateArray().Select(ToSpan).ToList();
    }

    private static AIExtractionSpan ToSpan(JsonElement pair)
    {
        if (pair.ValueKind != JsonValueKind.Array || pair.GetArrayLength() != 2)
        {
            throw new JsonException("Cloudflare returned a span that isn't a [start, end] pair.");
        }

        return new AIExtractionSpan(pair[0].GetInt32(), pair[1].GetInt32());
    }

    [GeneratedRegex(@"(Z|[+-]\d{2}:?\d{2})$", RegexOptions.IgnoreCase)]
    private static partial Regex OffsetSuffix();
}
