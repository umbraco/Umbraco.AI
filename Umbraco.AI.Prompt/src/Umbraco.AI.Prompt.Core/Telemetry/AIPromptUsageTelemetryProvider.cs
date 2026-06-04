using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Telemetry;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Infrastructure.Telemetry.Interfaces;

namespace Umbraco.AI.Prompt.Core.Telemetry;

/// <summary>
/// Contributes anonymous, aggregate Umbraco.AI.Prompt usage information to the CMS telemetry report.
/// </summary>
/// <remarks>
/// Data is only ever sent when the site's telemetry level is set to <c>Detailed</c>, and is
/// suppressed entirely when <c>Umbraco:AI:Telemetry:Enabled</c> is <c>false</c>. Only counts
/// and enum names are reported — see <see cref="AIPromptUsageTelemetryConstants"/> for the
/// complete whitelist.
/// </remarks>
public sealed class AIPromptUsageTelemetryProvider : IDetailedTelemetryProvider
{
    private readonly IOptionsMonitor<AIUsageTelemetryOptions> _telemetryOptions;
    private readonly IAIPromptService _promptService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIPromptUsageTelemetryProvider"/> class.
    /// </summary>
    public AIPromptUsageTelemetryProvider(
        IOptionsMonitor<AIUsageTelemetryOptions> telemetryOptions,
        IAIPromptService promptService)
    {
        _telemetryOptions = telemetryOptions;
        _promptService = promptService;
    }

    /// <inheritdoc />
    public IEnumerable<UsageInformation> GetInformation()
    {
        if (!_telemetryOptions.CurrentValue.Enabled)
        {
            return [];
        }

        try
        {
            AIPrompt[] prompts = _promptService
                .GetPromptsAsync()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult()
                .ToArray();

            var displayModes = prompts
                .Select(p => p.DisplayMode.ToString())
                .ToHashSet();

            return
            [
                new UsageInformation(AIPromptUsageTelemetryConstants.PromptCount, prompts.Length),
                new UsageInformation(AIPromptUsageTelemetryConstants.PromptActiveCount, prompts.Count(p => p.IsActive)),
                new UsageInformation(AIPromptUsageTelemetryConstants.PromptWithProfileCount, prompts.Count(p => p.ProfileId.HasValue)),
                new UsageInformation(AIPromptUsageTelemetryConstants.PromptWithContextCount, prompts.Count(p => p.ContextIds.Count > 0)),
                new UsageInformation(AIPromptUsageTelemetryConstants.PromptWithGuardrailCount, prompts.Count(p => p.GuardrailIds.Count > 0)),
                new UsageInformation(AIPromptUsageTelemetryConstants.PromptDisplayModes, displayModes),
            ];
        }
        catch
        {
            // Telemetry is strictly best-effort; never throw into the CMS ReportSiteJob.
            return [];
        }
    }
}
