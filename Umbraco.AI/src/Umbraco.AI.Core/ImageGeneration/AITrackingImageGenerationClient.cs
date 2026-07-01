using Microsoft.Extensions.AI;

#pragma warning disable MEAI001 // IImageGenerator is experimental in M.E.AI
#pragma warning disable UMBRACOAI_IMAGEGEN // Internal plumbing for the experimental image-generation API

namespace Umbraco.AI.Core.ImageGeneration;

/// <summary>
/// Middleware client that records usage analytics and audit entries around a normal pipeline
/// generation, by delegating to the shared <see cref="AIImageGenerationTracker"/>.
/// </summary>
/// <remarks>
/// The recording orchestration lives entirely in <see cref="AIImageGenerationTracker"/> so it is
/// shared with <see cref="IAIImageGenerationService.InvokeWithTrackingAsync{TResult}"/> (the
/// escape-hatch path) — there is a single implementation of "record an image-generation operation".
/// </remarks>
internal sealed class AITrackingImageGenerationClient : AIBoundImageGeneratorBase
{
    private readonly AIImageGenerationTracker _tracker;

    public AITrackingImageGenerationClient(IImageGenerator innerGenerator, AIImageGenerationTracker tracker)
        : base(innerGenerator)
    {
        _tracker = tracker;
    }

    /// <inheritdoc />
    public override async Task<ImageGenerationResponse> GenerateAsync(
        ImageGenerationRequest request,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var tracked = await _tracker.TrackAsync(
            BuildPromptData(request, options),
            async token =>
            {
                var response = await base.GenerateAsync(request, options, token);
                var imageCount = response.Contents?.Count(c => c is DataContent or UriContent) ?? 0;
                return new AITrackedImageResult<ImageGenerationResponse>
                {
                    Result = response,
                    Usage = response.Usage,
                    ImageCount = imageCount,
                };
            },
            cancellationToken);

        return tracked.Result;
    }

    /// <summary>
    /// Builds a descriptive prompt-data object captured for the audit entry.
    /// </summary>
    private static object BuildPromptData(ImageGenerationRequest request, ImageGenerationOptions? options)
        => new
        {
            Type = "image-generation",
            request.Prompt,
            IsEdit = request.OriginalImages is not null,
            options?.ModelId,
            Size = options?.ImageSize is { } s ? $"{s.Width}x{s.Height}" : null,
            options?.Count,
        };
}
