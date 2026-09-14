using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.Moonshot;

/// <summary>
/// AI chat capability for the Moonshot provider.
/// </summary>
public class MoonshotChatCapability(MoonshotProvider provider) : AIChatCapabilityBase<MoonshotProviderSettings>(provider)
{
    private const string DefaultChatModel = "kimi-k3";

    private new MoonshotProvider Provider => (MoonshotProvider)base.Provider;

    /// <summary>
    /// Patterns that match Moonshot (Kimi) chat models. Broad on purpose so new model
    /// families (e.g. kimi-k4-*) are picked up without code changes.
    /// </summary>
    private static readonly Regex[] IncludePatterns =
    [
        new(@"^kimi-", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        MoonshotProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelIdsAsync(settings, cancellationToken);

        return allModels
            .Where(IsChatModel)
            .Select(id => new AIModelDescriptor(
                new AIModelRef(Provider.Id, id),
                MoonshotModelUtilities.FormatDisplayName(id)))
            .ToList();
    }

    /// <inheritdoc />
    protected override IChatClient CreateClient(MoonshotProviderSettings settings, string? modelId)
    {
        return MoonshotProvider.CreateMoonshotClient(settings)
            .GetChatClient(modelId ?? DefaultChatModel)
            .AsIChatClient();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Every Kimi model rejects a non-default <c>temperature</c> outright (HTTP 400
    /// <c>invalid temperature: only 1 is allowed for this model</c>) rather than clamping or ignoring it,
    /// so a profile carrying any other value would fail every request. Declaring it unsupported here makes
    /// the base strip it before the request is sent — see <see cref="DeclaredSettingsChatClient"/> — so the
    /// API's own default (which satisfies the constraint) applies instead.
    /// </remarks>
    public override AIModelSettingsSupport GetSettingsSupport(string modelId)
        => new()
        {
            UnsupportedProfileSettings = [AIProfileSettingKeys.Temperature],
        };

    private static bool IsChatModel(string modelId)
        => IncludePatterns.Any(p => p.IsMatch(modelId));
}
