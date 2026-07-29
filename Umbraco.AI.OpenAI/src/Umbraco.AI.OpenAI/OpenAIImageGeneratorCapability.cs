#pragma warning disable MEAI001 // IImageGenerator / AsIImageGenerator are experimental in M.E.AI
#pragma warning disable UMBRACOAI_IMAGEGEN // Implements the experimental Umbraco.AI image-generation capability

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.AI.Core.ImageGeneration;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.OpenAI;

/// <summary>
/// AI image-generation capability for the OpenAI provider.
/// </summary>
/// <remarks>
/// Experimental — gated by the <c>Umbraco:AI:Experimental:ImageGeneration</c> feature flag and the
/// <c>UMBRACOAI_IMAGEGEN</c> diagnostic. Wraps the M.E.AI OpenAI image adapter in a decorator that also
/// exposes the un-bound <see cref="OpenAIClient"/> via <c>GetService</c>, so consumers can reach the
/// provider-native client for masked outpainting (Tier 3) while picking their own model/size at call time.
/// </remarks>
[Experimental(AIImageGenerationDiagnostics.DiagnosticId)]
public class OpenAIImageGeneratorCapability(
    OpenAIProvider provider,
    ILogger<OpenAIImageGeneratorCapability>? logger)
    : AIImageGeneratorCapabilityBase<OpenAIProviderSettings, OpenAIImageCapabilitySettings>(provider)
{
    /// <summary>
    /// Initializes a new instance without a logger.
    /// </summary>
    /// <remarks>
    /// Retained for construction paths that pass only the provider (the capability factory resolves the rest
    /// from DI, but plain activation does not). The logger is resolved through the service locator, following
    /// the same approach as the chat capability, so those paths still get real logging rather than none.
    /// Null-conditional because the locator is unset before startup and in unit tests.
    /// </remarks>
    public OpenAIImageGeneratorCapability(OpenAIProvider provider)
        : this(provider, StaticServiceProvider.Instance?.GetService<ILogger<OpenAIImageGeneratorCapability>>())
    {
    }

    private const string DefaultImageModel = "gpt-image-1";

    private new OpenAIProvider Provider => (OpenAIProvider)base.Provider;

    /// <summary>
    /// Patterns that match image-generation models.
    /// </summary>
    private static readonly Regex[] IncludePatterns =
    [
        new(@"^gpt-image", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"^dall-e", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    ];

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        OpenAIProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var allModels = await Provider.GetAvailableModelIdsAsync(settings, cancellationToken);

        return allModels
            .Where(IsImageModel)
            .Select(id => new AIModelDescriptor(
                new AIModelRef(Provider.Id, id),
                OpenAIModelUtilities.FormatDisplayName(id),
                GetImageConstraints(id)))
            .ToList();
    }

    /// <inheritdoc />
    protected override IImageGenerator CreateGenerator(OpenAIProviderSettings settings, string? modelId)
    {
        var client = OpenAIProvider.CreateOpenAIClient(settings);
        var inner = client.GetImageClient(modelId ?? DefaultImageModel).AsIImageGenerator();

        // Wrap so consumers can also resolve the un-bound OpenAIClient (the stock adapter only exposes the
        // bound ImageClient), enabling provider-native masked outpainting via GetService.
        var generator = new OpenAIImageGenerator(inner, client);

        // Wrapped outside that, so hints passed as additional properties by a direct consumer are still
        // translated — the adapter ignores them otherwise. A profile's own settings arrive typed through
        // ApplyCapabilitySettings instead. See OpenAIImageHintGenerator.
        return new OpenAIImageHintGenerator(generator, modelId ?? DefaultImageModel, logger);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Style is DALL·E 3 only, so it is declared unsupported elsewhere and the profile editor drops the
    /// field. Quality applies to every family but with different vocabularies, which
    /// <see cref="ApplyCapabilitySettings"/> gates per request — a per-model list of allowed *values* would
    /// need a declaration channel the model metadata does not have yet.
    /// </remarks>
    public override AIModelSettingsSupport GetSettingsSupport(string modelId)
        => OpenAIImageHints.SupportsStyle(modelId)
            ? AIModelSettingsSupport.Default
            : new AIModelSettingsSupport
            {
                UnsupportedCapabilitySettings = [nameof(OpenAIImageCapabilitySettings.Style)],
            };

    /// <inheritdoc />
    /// <remarks>
    /// Translates the profile's hints into the SDK's own options, which is the only channel the adapter
    /// reads. Gated with the same predicates that drive <see cref="GetSettingsSupport"/>, so a profile
    /// carrying a value the selected model rejects degrades rather than failing the request.
    /// </remarks>
    protected override void ApplyCapabilitySettings(
        OpenAIImageCapabilitySettings capabilitySettings,
        string? modelId,
        ImageGenerationOptions options)
    {
        if (options.RawRepresentationFactory is not null)
        {
            // A caller that built the SDK options itself is more specific than a profile-level default.
            return;
        }

        var factory = OpenAIImageHints.CreateRawFactory(
            capabilitySettings.Quality,
            capabilitySettings.Style,
            modelId ?? DefaultImageModel,
            logger);

        if (factory is not null)
        {
            options.RawRepresentationFactory = factory;
        }
    }

    private static bool IsImageModel(string modelId)
        => IncludePatterns.Any(p => p.IsMatch(modelId));

    /// <summary>
    /// Per-model image constraints, surfaced via <see cref="AIModelDescriptor.Metadata"/> so consumers can
    /// validate sizes up front and fail clearly rather than getting a wrong-ratio result.
    /// </summary>
    private static IReadOnlyDictionary<string, string> GetImageConstraints(string modelId)
    {
        var id = modelId.ToLowerInvariant();

        if (id.StartsWith("gpt-image"))
        {
            return new Dictionary<string, string>
            {
                [AIModelMetadataKeys.ImageSupportedSizes] = "1024x1024,1024x1536,1536x1024",
                [AIModelMetadataKeys.ImageMaxEdge] = "1536",
                [AIModelMetadataKeys.ImageSupportsEdit] = "true",
                [AIModelMetadataKeys.ImageSupportsMask] = "true",
            };
        }

        if (id.StartsWith("dall-e-3"))
        {
            return new Dictionary<string, string>
            {
                [AIModelMetadataKeys.ImageSupportedSizes] = "1024x1024,1792x1024,1024x1792",
                [AIModelMetadataKeys.ImageMaxEdge] = "1792",
                [AIModelMetadataKeys.ImageSupportsEdit] = "false",
                [AIModelMetadataKeys.ImageSupportsMask] = "false",
            };
        }

        if (id.StartsWith("dall-e-2"))
        {
            return new Dictionary<string, string>
            {
                [AIModelMetadataKeys.ImageSupportedSizes] = "256x256,512x512,1024x1024",
                [AIModelMetadataKeys.ImageMaxEdge] = "1024",
                [AIModelMetadataKeys.ImageSupportsEdit] = "true",
                [AIModelMetadataKeys.ImageSupportsMask] = "true",
            };
        }

        return new Dictionary<string, string>();
    }
}

/// <summary>
/// Decorator over the M.E.AI OpenAI image generator that additionally resolves the un-bound
/// <see cref="OpenAIClient"/> through <c>GetService</c>.
/// </summary>
/// <remarks>
/// The stock adapter's <c>GetService</c> returns only the bound <c>ImageClient</c> (which cannot self-report
/// its model/sizes). Exposing the <see cref="OpenAIClient"/> lets a consumer call
/// <c>.GetImageClient("gpt-image-1")</c> and choose model/size at call time for masked outpainting.
/// </remarks>
internal sealed class OpenAIImageGenerator : DelegatingImageGenerator
{
    private readonly OpenAIClient _client;

    public OpenAIImageGenerator(IImageGenerator innerGenerator, OpenAIClient client)
        : base(innerGenerator)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <inheritdoc />
    public override object? GetService(Type serviceType, object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (serviceKey is null && serviceType == typeof(OpenAIClient))
        {
            return _client;
        }

        // Delegates to the inner adapter, which resolves the bound ImageClient + ImageGeneratorMetadata.
        return base.GetService(serviceType, serviceKey);
    }
}
