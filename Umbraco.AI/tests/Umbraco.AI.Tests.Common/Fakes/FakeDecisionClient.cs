#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability

using Umbraco.AI.Core.Decision;

namespace Umbraco.AI.Tests.Common.Fakes;

/// <summary>
/// Fake implementation of <see cref="IAIDecisionClient"/> for use in tests.
/// </summary>
public class FakeDecisionClient : IAIDecisionClient
{
    private readonly Func<AIDecisionRequest, AIDecisionResponse> _respond;

    public FakeDecisionClient(Func<AIDecisionRequest, AIDecisionResponse>? respond = null)
    {
        _respond = respond ?? (request => new AIDecisionResponse
        {
            Answers = request.Questions.ToDictionary(
                q => q.Id ?? "answer",
                _ => (AIDecisionAnswer)new AIBinaryDecisionAnswer { TrueProbability = 0.9 }),
        });
    }

    /// <summary>
    /// Gets the list of (request, options) pairs that were passed to <see cref="GetResponseAsync"/>.
    /// </summary>
    public List<(AIDecisionRequest Request, AIDecisionOptions? Options)> ReceivedRequests { get; } = [];

    public Task<AIDecisionResponse> GetResponseAsync(
        AIDecisionRequest request,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ReceivedRequests.Add((request, options));
        return Task.FromResult(_respond(request));
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
