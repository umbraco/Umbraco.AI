using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.OpenAI;

/// <summary>
/// AI chat capability for OpenAI provider.
/// </summary>
public class OpenAIChatCapability(
    OpenAIProvider provider,
    ILogger<OpenAIChatCapability>? logger)
    : AIChatCapabilityBase<OpenAIProviderSettings>(provider)
{
    /// <summary>
    /// Initializes a new instance without a logger.
    /// </summary>
    /// <remarks>
    /// Retained so adding the logger parameter stays binary compatible. An optional parameter would not
    /// achieve that — the compiler emits a single constructor and bakes the default in at each call site,
    /// so assemblies compiled against the previous signature would fail to bind.
    /// </remarks>
    public OpenAIChatCapability(OpenAIProvider provider)
        : this(provider, null)
    {
    }

    private const string DefaultChatModel = "gpt-4o";

    private new OpenAIProvider Provider => (OpenAIProvider)base.Provider;

    /// <summary>
    /// Patterns that match chat/completion models.
    /// </summary>
    private static readonly Regex[] IncludePatterns =
    [
        new(@"^gpt-", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^o1", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^o3", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^chatgpt-", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <summary>
    /// Patterns to exclude from chat models (e.g., realtime, audio-only variants).
    /// </summary>
    private static readonly Regex[] ExcludePatterns =
    [
        new(@"-realtime", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^gpt-4o-transcribe", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^gpt-4o-mini-transcribe", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        OpenAIProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelIdsAsync(settings, cancellationToken);

        return allModels
            .Where(IsChatModel)
            .Select(id => new AIModelDescriptor(
                new AIModelRef(Provider.Id, id),
                OpenAIModelUtilities.FormatDisplayName(id)))
            .ToList();
    }

    /// <inheritdoc />
    [Experimental("OPENAI001")]
    protected override IChatClient CreateClient(OpenAIProviderSettings settings, string? modelId)
    {
        var resolvedModelId = modelId ?? DefaultChatModel;

        var inner = OpenAIProvider.CreateOpenAIClient(settings)
            .GetResponsesClient()
            .AsIChatClient(resolvedModelId);

        // Wrapped innermost, so the sampling parameters are filtered against the target model no matter
        // which caller assembled the ChatOptions. See OpenAISamplingParameterChatClient.
        return new OpenAISamplingParameterChatClient(inner, resolvedModelId, logger);
    }

    private static bool IsChatModel(string modelId)
        => IncludePatterns.Any(p => p.IsMatch(modelId))
           && !ExcludePatterns.Any(p => p.IsMatch(modelId));
}
