#pragma warning disable MEAI001 // IImageGenerator / AsIImageGenerator are experimental in M.E.AI
#pragma warning disable UMBRACOAI_IMAGEGEN // Implements the experimental Umbraco.AI image-generation capability

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using OpenAI;
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
public class OpenAIImageGeneratorCapability(OpenAIProvider provider)
    : AIImageGeneratorCapabilityBase<OpenAIProviderSettings>(provider)
{
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
        return new OpenAIImageGenerator(inner, client);
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
                ["image.supportedSizes"] = "1024x1024,1024x1536,1536x1024",
                ["image.maxEdge"] = "1536",
                ["image.supportsEdit"] = "true",
                ["image.supportsMask"] = "true",
            };
        }

        if (id.StartsWith("dall-e-3"))
        {
            return new Dictionary<string, string>
            {
                ["image.supportedSizes"] = "1024x1024,1792x1024,1024x1792",
                ["image.maxEdge"] = "1792",
                ["image.supportsEdit"] = "false",
                ["image.supportsMask"] = "false",
            };
        }

        if (id.StartsWith("dall-e-2"))
        {
            return new Dictionary<string, string>
            {
                ["image.supportedSizes"] = "256x256,512x512,1024x1024",
                ["image.maxEdge"] = "1024",
                ["image.supportsEdit"] = "true",
                ["image.supportsMask"] = "true",
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
