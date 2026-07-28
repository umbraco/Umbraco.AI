using System.Text.RegularExpressions;
using Amazon.BedrockRuntime;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Amazon;

/// <summary>
/// AI chat capability for Amazon Bedrock provider.
/// </summary>
public class AmazonChatCapability(
    AmazonProvider provider,
    ILogger<AmazonChatCapability>? logger)
    : AIChatCapabilityBase<AmazonProviderSettings>(provider)
{
    /// <summary>
    /// Initializes a new instance without a logger.
    /// </summary>
    /// <remarks>
    /// Retained so adding the logger parameter stays binary compatible. An optional parameter would not
    /// achieve that — the compiler emits a single constructor and bakes the default in at each call site,
    /// so assemblies compiled against the previous signature would fail to bind.
    /// <para>
    /// The logger is resolved through the service locator, following the same approach as
    /// <c>AIGuardrailEvaluatorBase</c>, so a consumer still on this signature gets real logging rather than
    /// none. Null-conditional because the locator is unset before startup and in unit tests, and logging is
    /// optional here — unlike the required services that pattern usually resolves.
    /// </para>
    /// </remarks>
    [Obsolete("Use the constructor that accepts a logger. Will be removed in v20.")]
    public AmazonChatCapability(AmazonProvider provider)
        : this(provider, StaticServiceProvider.Instance?.GetService<ILogger<AmazonChatCapability>>())
    {
    }

    /// <summary>
    /// Optional region prefix pattern for inference profile IDs (e.g., "eu.", "us.", "apac.").
    /// </summary>
    private const string RegionPrefixPattern = @"(eu\.|us\.|apac\.)?";

    private new AmazonProvider Provider => (AmazonProvider)base.Provider;

    /// <summary>
    /// Patterns that match chat models in Bedrock (with optional region prefix for inference profiles).
    /// </summary>
    private static readonly Regex[] IncludePatterns =
    [
        new($@"^{RegionPrefixPattern}amazon\.nova-", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new($@"^{RegionPrefixPattern}anthropic\.claude-", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new($@"^{RegionPrefixPattern}mistral\.", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new($@"^{RegionPrefixPattern}meta\.llama", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    ];

    /// <summary>
    /// Patterns that exclude non-chat models.
    /// </summary>
    private static readonly Regex[] ExcludePatterns =
    [
        new(@"embed", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        AmazonProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelIdsAsync(settings, cancellationToken);

        return allModels
            .Where(IsChatModel)
            .Select(id => new AIModelDescriptor(
                new AIModelRef(Provider.Id, id),
                AmazonModelUtilities.FormatDisplayName(id)))
            .ToList();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Bedrock fronts other vendors' models, so it inherits their restrictions: the Claude families that
    /// reject <c>temperature</c> reject it here too. <see cref="AmazonSamplingParameterChatClient"/>
    /// already drops it at request time from the predicate used here, so declaring it lets the editor
    /// account for the drop instead of leaving the profile with a value that does nothing.
    /// </remarks>
    public override AIModelSettingsSupport GetSettingsSupport(string modelId)
        => AmazonModelUtilities.SupportsSamplingParameters(modelId)
            ? AIModelSettingsSupport.Default
            : new AIModelSettingsSupport
            {
                UnsupportedProfileSettings = [nameof(AIChatProfileSettings.Temperature)],
            };

    /// <inheritdoc />
    protected override IChatClient CreateClient(AmazonProviderSettings settings, string? modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new InvalidOperationException(
                "A model must be selected for Amazon Bedrock. " +
                "Please select a model from the available inference profiles.");
        }

        var client = AmazonProvider.CreateBedrockRuntimeClient(settings);

        // Wrapped innermost, so the sampling parameters are filtered against the target model no matter
        // which caller assembled the ChatOptions. See AmazonSamplingParameterChatClient.
        return new AmazonSamplingParameterChatClient(client.AsIChatClient(modelId), modelId, logger);
    }

    private static bool IsChatModel(string modelId)
        => IncludePatterns.Any(p => p.IsMatch(modelId))
           && !ExcludePatterns.Any(p => p.IsMatch(modelId));
}
