using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.MicrosoftFoundry;

/// <summary>
/// AI embedding capability for Microsoft AI Foundry provider.
/// </summary>
/// <remarks>
/// Supports all embedding models available through Microsoft AI Foundry, including
/// OpenAI (text-embedding-3-small, text-embedding-3-large) and other models.
/// </remarks>
public class MicrosoftFoundryEmbeddingCapability(
    MicrosoftFoundryProvider provider,
    ILogger<MicrosoftFoundryEmbeddingCapability>? logger)
    : AIEmbeddingCapabilityBase<MicrosoftFoundryProviderSettings>(provider)
{
    /// <summary>
    /// Initializes a new instance without a logger.
    /// </summary>
    /// <remarks>
    /// Retained so adding the logger parameter stays binary compatible. An optional parameter would not
    /// achieve that — the compiler emits a single constructor and bakes the default in at each call site,
    /// so assemblies compiled against the previous signature would fail to bind. The logger is resolved
    /// through the service locator so a consumer still on this signature gets real logging rather than none;
    /// null-conditional because the locator is unset before startup and in unit tests.
    /// </remarks>
    [Obsolete("Use the constructor that accepts a logger. Will be removed in v20.")]
    public MicrosoftFoundryEmbeddingCapability(MicrosoftFoundryProvider provider)
        : this(provider, StaticServiceProvider.Instance?.GetService<ILogger<MicrosoftFoundryEmbeddingCapability>>())
    {
    }

    private const string DefaultEmbeddingModel = "text-embedding-3-small";

    private new MicrosoftFoundryProvider Provider => (MicrosoftFoundryProvider)base.Provider;

    /// <inheritdoc />
    /// <remarks>
    /// Declarations are attached here from the same predicate <see cref="GetSettingsSupport"/> uses, with
    /// each model's listing entry passed in directly rather than read back from the provider's cache.
    /// </remarks>
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        MicrosoftFoundryProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelsAsync(settings, cancellationToken);

        return allModels
            .Where(IsEmbeddingModel)
            .Select(m => new AIModelDescriptor(
                new AIModelRef(Provider.Id, m.Id),
                MicrosoftFoundryModelUtilities.FormatDisplayName(m.Id, m.ModelName, m.ModelVersion),
                BuildSettingsSupport(m.Id, m).ToMetadata()))
            .ToList();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Shortened embeddings are a <c>text-embedding-3</c> feature, so a profile's Dimensions cannot apply to
    /// an <c>ada-002</c> deployment. Declaring it hides the field for those models and, because the base
    /// enforces what is declared, also strips the value from a request that still carries one.
    /// </remarks>
    public override AIModelSettingsSupport GetSettingsSupport(string modelId)
        => BuildSettingsSupport(modelId, Provider.TryGetModelInfo(modelId));

    /// <summary>
    /// Turns a model's listing entry into the settings declaration the editor reads and the base enforces.
    /// </summary>
    /// <remarks>
    /// A <c>null</c> entry — the models API path, or a model absent from the last listing — falls back to
    /// reasoning from the ID.
    /// </remarks>
    private static AIModelSettingsSupport BuildSettingsSupport(string modelId, MicrosoftFoundryModelInfo? info)
        => MicrosoftFoundryModelUtilities.SupportsDimensions(modelId, info?.ModelName, info?.ModelPublisher)
            ? AIModelSettingsSupport.Default
            : new AIModelSettingsSupport
            {
                UnsupportedProfileSettings = [AIProfileSettingKeys.Dimensions],
            };

    /// <inheritdoc />
    /// <remarks>
    /// Prefetches the model list for the same reason the chat capability does: so the declaration the base
    /// enforces can be made against the model a deployment fronts rather than its name. A failure degrades
    /// to reasoning from the ID and logs, since losing the listing must not stop embedding from working.
    /// </remarks>
    protected override async Task<IEmbeddingGenerator<string, Embedding<float>>> CreateGeneratorAsync(
        MicrosoftFoundryProviderSettings settings,
        string? modelId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await Provider.GetAvailableModelsAsync(settings, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger?.LogDebug(
                ex,
                "Could not list Microsoft AI Foundry models while creating an embedding generator. The "
                + "model a deployment fronts is unavailable, so setting support will be inferred from the "
                + "model ID instead.");
        }

        // The declaration from GetSettingsSupport is enforced by the base, which wraps this generator so
        // Dimensions is stripped for a model that rejects it. See DeclaredSettingsEmbeddingGenerator.
        return MicrosoftFoundryProvider.CreateAzureOpenAIClient(settings)
            .GetEmbeddingClient(modelId ?? DefaultEmbeddingModel)
            .AsIEmbeddingGenerator();
    }

    private static bool IsEmbeddingModel(MicrosoftFoundryModelInfo model)
    {
        // If capabilities are provided, use them
        if (model.Capabilities is not null)
        {
            return model.Capabilities.Embeddings;
        }

        // Fallback: look for embedding model patterns
        var id = model.Id.ToLowerInvariant();
        return id.Contains("embedding") || id.Contains("embed");
    }
}
