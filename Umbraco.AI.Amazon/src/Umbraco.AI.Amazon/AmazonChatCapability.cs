using System.Text.RegularExpressions;
using Amazon.BedrockRuntime;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.Amazon;

/// <summary>
/// AI chat capability for Amazon Bedrock provider.
/// </summary>
public class AmazonChatCapability(AmazonProvider provider) : AIChatCapabilityBase<AmazonProviderSettings>(provider)
{
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
    protected override IChatClient CreateClient(AmazonProviderSettings settings, string? modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new InvalidOperationException(
                "A model must be selected for Amazon Bedrock. " +
                "Please select a model from the available inference profiles.");
        }

        var client = AmazonProvider.CreateBedrockRuntimeClient(settings);
        return client.AsIChatClient(modelId);
    }

    private static bool IsChatModel(string modelId)
        => IncludePatterns.Any(p => p.IsMatch(modelId))
           && !ExcludePatterns.Any(p => p.IsMatch(modelId));
}
