using Umbraco.Cms.Core;

namespace Umbraco.AI.Agent.Core.Agents;

/// <inheritdoc />
internal sealed class AIAgentUserGroupResolutionService : IAIAgentUserGroupResolutionService
{
    // Built once, on first use (thread-safe). A duplicate kind throws here, and every call after it
    // throws the same exception, so a misconfigured site never silently picks one resolver.
    private readonly Lazy<IReadOnlyDictionary<string, IAIAgentUserGroupResolver>> _resolversByKind;

    public AIAgentUserGroupResolutionService(AIAgentUserGroupResolverCollection resolvers)
        => _resolversByKind = new Lazy<IReadOnlyDictionary<string, IAIAgentUserGroupResolver>>(
            () => IndexByKind(resolvers));

    /// <inheritdoc />
    public async Task<Attempt<IReadOnlyList<Guid>, AIAgentUserGroupResolutionStatus>> ResolveUserGroupsAsync(
        string onBehalfOf,
        CancellationToken cancellationToken = default)
    {
        // The kind is the text before the first colon; everything after it is the resolver's value.
        var separator = onBehalfOf?.IndexOf(':') ?? -1;
        if (onBehalfOf is null || separator < 0)
        {
            return Fail(AIAgentUserGroupResolutionStatus.Malformed);
        }

        var kind = onBehalfOf[..separator];
        var value = onBehalfOf[(separator + 1)..];
        if (string.IsNullOrWhiteSpace(kind) || string.IsNullOrWhiteSpace(value))
        {
            return Fail(AIAgentUserGroupResolutionStatus.Malformed);
        }

        if (!_resolversByKind.Value.TryGetValue(kind, out var resolver))
        {
            return Fail(AIAgentUserGroupResolutionStatus.NotHandled);
        }

        return Normalize(await resolver.ResolveUserGroupsAsync(value, cancellationToken));
    }

    private static IReadOnlyDictionary<string, IAIAgentUserGroupResolver> IndexByKind(
        IEnumerable<IAIAgentUserGroupResolver> resolvers)
    {
        var byKind = new Dictionary<string, IAIAgentUserGroupResolver>(StringComparer.OrdinalIgnoreCase);
        foreach (var resolver in resolvers)
        {
            if (!byKind.TryAdd(resolver.Kind, resolver))
            {
                throw new InvalidOperationException(
                    $"Agent user group resolvers {byKind[resolver.Kind].GetType().FullName} and "
                    + $"{resolver.GetType().FullName} both claim the onBehalfOf kind '{resolver.Kind}'. "
                    + "Use Replace<TOld, TNew>() or Remove<T>() on the collection builder so each kind has one resolver.");
            }
        }

        return byKind;
    }

    // The resolver is the authorization check, so anything it returns that isn't clearly a grant,
    // Forbidden or NoUser (a contradiction, a null list, Malformed, NotHandled) is treated as Forbidden.
    private static Attempt<IReadOnlyList<Guid>, AIAgentUserGroupResolutionStatus> Normalize(
        Attempt<IReadOnlyList<Guid>, AIAgentUserGroupResolutionStatus> result)
    {
        if (result.Success)
        {
            return result.Status == AIAgentUserGroupResolutionStatus.Success && result.Result is not null
                ? result
                : Fail(AIAgentUserGroupResolutionStatus.Forbidden);
        }

        return result.Status is AIAgentUserGroupResolutionStatus.Forbidden or AIAgentUserGroupResolutionStatus.NoUser
            ? Fail(result.Status)
            : Fail(AIAgentUserGroupResolutionStatus.Forbidden);
    }

    private static Attempt<IReadOnlyList<Guid>, AIAgentUserGroupResolutionStatus> Fail(
        AIAgentUserGroupResolutionStatus status)
        => Attempt.FailWithStatus<IReadOnlyList<Guid>, AIAgentUserGroupResolutionStatus>(status, []);
}
