using Umbraco.AI.Core.Analytics.Usage;
using Umbraco.AI.Core.Observability;
using Umbraco.Cms.Core.Security;

namespace Umbraco.AI.Core.Telemetry;

/// <summary>
/// Tags a tracked call's own gen_ai span with Umbraco AI context (profile, user, entity, feature, audit entry),
/// so traces can be filtered by them.
/// </summary>
/// <remarks>
/// The span doesn't exist yet when the call starts. The tags are built then and made current around the call's
/// work; <see cref="AITraceTags.Apply"/> puts them on the span when the OpenTelemetry middleware starts it.
/// </remarks>
internal sealed class AITraceOperationRecorder : IAIOperationRecorder
{
    private readonly IBackOfficeSecurityAccessor _securityAccessor;

    public AITraceOperationRecorder(IBackOfficeSecurityAccessor securityAccessor)
        => _securityAccessor = securityAccessor;

    public ValueTask<IAIOperationRecording?> BeginAsync(AIOperationStart start, CancellationToken cancellationToken)
    {
        // Always a recording, even with no tags: entering it keeps a nested call from showing its parent's tags.
        var tags = start.Identity is { } identity ? BuildTags(identity) : [];
        return ValueTask.FromResult<IAIOperationRecording?>(new Recording(tags));
    }

    private Dictionary<string, string> BuildTags(AIUsageContext identity)
    {
        var tags = new Dictionary<string, string>();

        // Missing IDs are read from the runtime context as Guid.Empty, so treat that as absent.
        if (identity.ProfileId is { } profileId && profileId != Guid.Empty)
        {
            tags[AITelemetry.Tags.ProfileId] = profileId.ToString();
        }

        AddIfPresent(tags, AITelemetry.Tags.ProfileAlias, identity.ProfileAlias);
        AddIfPresent(tags, AITelemetry.Tags.UserId, _securityAccessor.BackOfficeSecurity?.CurrentUser?.Key.ToString());
        AddIfPresent(tags, AITelemetry.Tags.EntityId, identity.EntityId);
        AddIfPresent(tags, AITelemetry.Tags.EntityType, identity.EntityType);
        AddIfPresent(tags, AITelemetry.Tags.FeatureType, identity.FeatureType);

        if (identity.FeatureId is { } featureId && featureId != Guid.Empty)
        {
            tags[AITelemetry.Tags.FeatureId] = featureId.ToString();
        }

        return tags;
    }

    private static void AddIfPresent(Dictionary<string, string> tags, string tag, string? value)
    {
        if (value is not null)
        {
            tags[tag] = value;
        }
    }

    private sealed class Recording(IReadOnlyDictionary<string, string> tags) : IAIOperationRecording
    {
        public IDisposable EnterScope() => AITraceTags.Enter(tags);

        // Nothing to record at the end.
        public ValueTask EndAsync(AIOperationOutcome outcome) => ValueTask.CompletedTask;
    }
}
