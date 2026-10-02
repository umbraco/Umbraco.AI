#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability

using Umbraco.AI.Core.Decision;

namespace Umbraco.AI.Tests.Common.Fakes;

/// <summary>
/// Fake implementation of <see cref="IAIDecisionClient"/> for use in tests.
/// </summary>
public class FakeDecisionClient : IAIDecisionClient
{
    private readonly Func<AIDecisionQuestion, AIDecisionResponse> _respond;

    public FakeDecisionClient(Func<AIDecisionQuestion, AIDecisionResponse>? respond = null)
    {
        _respond = respond ?? (q => new AIBinaryDecisionResponse { Probability = 0.9 });
    }

    /// <summary>
    /// Gets the list of (question, options) pairs that were passed to <see cref="AskAsync"/>.
    /// </summary>
    public List<(AIDecisionQuestion Question, AIDecisionOptions? Options)> ReceivedRequests { get; } = [];

    public Task<AIDecisionResponse> AskAsync(
        AIDecisionQuestion question,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ReceivedRequests.Add((question, options));
        return Task.FromResult(_respond(question));
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceType == typeof(IAIDecisionClient) || serviceType == typeof(FakeDecisionClient))
        {
            return this;
        }

        return null;
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}
