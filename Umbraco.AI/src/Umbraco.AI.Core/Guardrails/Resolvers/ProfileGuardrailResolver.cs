using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Guardrails.Resolvers;

/// <summary>
/// Resolves guardrails from profile-level guardrail assignments.
/// </summary>
/// <remarks>
/// This resolver reads the profile ID from <see cref="Constants.ContextKeys.ProfileId"/> in the runtime context,
/// then resolves any guardrail IDs configured on the profile's chat settings.
/// </remarks>
internal sealed class ProfileGuardrailResolver : IAIGuardrailResolver
{
    private readonly IAIRuntimeContextAccessor _runtimeContextAccessor;
    private readonly IAIGuardrailService _guardrailService;
    private readonly IAIProfileService _profileService;

    public ProfileGuardrailResolver(
        IAIRuntimeContextAccessor runtimeContextAccessor,
        IAIGuardrailService guardrailService,
        IAIProfileService profileService)
    {
        _runtimeContextAccessor = runtimeContextAccessor;
        _guardrailService = guardrailService;
        _profileService = profileService;
    }

    /// <inheritdoc />
    public async Task<AIGuardrailResolverResult> ResolveAsync(CancellationToken cancellationToken = default)
    {
        // Override suppresses source-level guardrails entirely.
        if (_runtimeContextAccessor.Context?.GetValue<IReadOnlyList<Guid>>(Constants.ContextKeys.GuardrailIdsOverride) is not null)
        {
            return AIGuardrailResolverResult.Empty;
        }

        var profileId = _runtimeContextAccessor.Context?.GetValue<Guid>(Constants.ContextKeys.ProfileId);
        if (!profileId.HasValue)
        {
            return AIGuardrailResolverResult.Empty;
        }

        var profile = await _profileService.GetProfileAsync(profileId.Value, cancellationToken);
        if (profile?.Settings is not AIChatProfileSettings chatSettings || chatSettings.GuardrailIds.Count == 0)
        {
            return AIGuardrailResolverResult.Empty;
        }

        var guardrails = await _guardrailService.GetGuardrailsByIdsAsync(chatSettings.GuardrailIds, cancellationToken);
        return AIGuardrailResolverResult.FromGuardrails(guardrails, source: profile.Name);
    }
}
