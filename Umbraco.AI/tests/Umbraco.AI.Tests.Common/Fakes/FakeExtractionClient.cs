#pragma warning disable UMBRACOAI_EXTRACTION // Exercises the experimental extraction capability

using Umbraco.AI.Core.Extraction;

namespace Umbraco.AI.Tests.Common.Fakes;

/// <summary>
/// Fake implementation of <see cref="IAIExtractionClient"/> for use in tests.
/// </summary>
/// <remarks>
/// With no responder it answers every field with a found answer of the field's own type, so a spec that
/// only cares about the pipeline (validation, tracking, notifications) doesn't need to build answers.
/// </remarks>
public class FakeExtractionClient : IAIExtractionClient
{
    private readonly Func<AIExtractionRequest, AIExtractionResponse> _respond;

    public FakeExtractionClient(Func<AIExtractionRequest, AIExtractionResponse>? respond = null)
    {
        _respond = respond ?? (request => new AIExtractionResponse
        {
            Answers = request.Fields.ToDictionary(f => f.Id ?? "answer", DefaultAnswerFor),
        });
    }

    /// <summary>
    /// Gets the list of (request, options) pairs that were passed to <see cref="GetResponseAsync"/>.
    /// </summary>
    public List<(AIExtractionRequest Request, AIExtractionOptions? Options)> ReceivedRequests { get; } = [];

    public Task<AIExtractionResponse> GetResponseAsync(
        AIExtractionRequest request,
        AIExtractionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ReceivedRequests.Add((request, options));
        return Task.FromResult(_respond(request));
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == typeof(IAIExtractionClient) || serviceType == typeof(FakeExtractionClient))
        {
            return this;
        }

        return null;
    }

    public void Dispose()
    {
        // Nothing to dispose
    }

    private static AIExtractionAnswer DefaultAnswerFor(AIExtractionField field) => field switch
    {
        AITextExtractionField => new AITextExtractionAnswer { Value = "value" },
        AINumberExtractionField => new AINumberExtractionAnswer { Value = 1m },
        AIDateExtractionField => new AIDateExtractionAnswer { Value = new DateOnly(2026, 10, 3) },
        AITimeExtractionField => new AITimeExtractionAnswer { Value = new TimeOnly(14, 30) },
        AIDateTimeExtractionField => new AIDateTimeExtractionAnswer { Value = new DateTime(2026, 10, 3, 14, 30, 0) },
        AIDurationExtractionField duration => new AIDurationExtractionAnswer { Value = 1m, Unit = duration.Unit },
        AIListExtractionField => new AIListExtractionAnswer { Values = ["value"], Spans = [new AIExtractionSpan(0, 5)] },
        _ => throw new NotSupportedException($"FakeExtractionClient has no default answer for {field.GetType().Name}."),
    };
}
