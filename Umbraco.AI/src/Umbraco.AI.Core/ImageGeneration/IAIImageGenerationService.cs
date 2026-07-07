using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;

#pragma warning disable MEAI001 // IImageGenerator and image types are experimental in M.E.AI

namespace Umbraco.AI.Core.ImageGeneration;

/// <summary>
/// Defines an AI image-generation service that provides access to text-to-image and image-editing
/// capabilities. This service acts as a thin layer over Microsoft.Extensions.AI, adding Umbraco-specific
/// features like profiles, connections, and configurable middleware.
/// </summary>
/// <remarks>
/// Image generation is experimental — it is hidden unless the <c>Umbraco:AI:Experimental:ImageGeneration</c>
/// feature flag is enabled, and the API surface carries <see cref="ExperimentalAttribute"/>
/// (<c>UMBRACOAI_IMAGEGEN</c>), which consumers must suppress to use.
/// </remarks>
[Experimental(AIImageGenerationDiagnostics.DiagnosticId)]
public interface IAIImageGenerationService
{
    /// <summary>
    /// Generates images from a text prompt using an inline image-generation builder with full observability
    /// (notifications, telemetry, duration tracking).
    /// </summary>
    /// <param name="configure">Action to configure the inline image generation via the builder.</param>
    /// <param name="prompt">The text prompt describing the desired image(s).</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The image-generation response containing the generated image content.</returns>
    Task<ImageGenerationResponse> GenerateImagesAsync(
        Action<AIImageGenerationBuilder> configure,
        string prompt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates images from a text prompt and optional original images (maskless edit — Tier 2)
    /// using an inline image-generation builder with full observability.
    /// </summary>
    /// <param name="configure">Action to configure the inline image generation via the builder.</param>
    /// <param name="prompt">The text prompt describing the desired transformation.</param>
    /// <param name="originalImages">
    /// Optional original images to edit. When null/empty this behaves like text-to-image generation.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The image-generation response containing the generated image content.</returns>
    Task<ImageGenerationResponse> GenerateImagesAsync(
        Action<AIImageGenerationBuilder> configure,
        string prompt,
        IEnumerable<AIContent>? originalImages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a reusable inline image generator with scope management per-call.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The returned generator manages runtime context scopes automatically — each call to
    /// <c>GenerateAsync</c> creates a fresh scope, sets inline image-generation metadata, delegates,
    /// and disposes.
    /// </para>
    /// <para>
    /// <strong>Escape hatch:</strong> the returned generator forwards <c>GetService</c> through the full
    /// pipeline, so a consumer can resolve the provider-native client for masked outpainting (Tier 3),
    /// e.g. <c>generator.GetService(typeof(OpenAI.Images.ImageClient))</c> or
    /// <c>generator.GetService(typeof(OpenAI.OpenAIClient))</c>. This is OpenAI/Azure-OpenAI specific;
    /// other providers' <c>GetService</c> will not return those clients. Raw calls made this way bypass
    /// the usage/audit middleware — use <see cref="InvokeWithTrackingAsync{TResult}"/> to keep them visible.
    /// </para>
    /// <para>
    /// <strong>Note:</strong> Calling methods on the returned generator does not publish
    /// <see cref="AIImageGenerationExecutingNotification"/> or <see cref="AIImageGenerationExecutedNotification"/>.
    /// </para>
    /// </remarks>
    /// <param name="configure">Action to configure the inline image generation via the builder.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>A configured <see cref="IImageGenerator"/> with inline scope management.</returns>
    Task<IImageGenerator> CreateImageGeneratorAsync(
        Action<AIImageGenerationBuilder> configure,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs a consumer-supplied operation against the scoped image generator while recording usage and
    /// audit entries around it — keeping raw provider-native calls (e.g. masked outpainting) visible in
    /// analytics and audit even though they bypass the middleware pipeline.
    /// </summary>
    /// <remarks>
    /// Opens a runtime-context scope (populating profile/model/provider), builds the scoped generator,
    /// and invokes <paramref name="operation"/> with it. The delegate is expected to resolve the
    /// provider-native client via <c>GetService</c> and perform the raw call, returning an
    /// <see cref="AITrackedImageResult{TResult}"/> with any usage/image-count. The service records a usage
    /// record and audit start/complete on success, or a failure on exception.
    /// </remarks>
    /// <typeparam name="TResult">The caller-defined result type.</typeparam>
    /// <param name="configure">Action to configure the inline image generation via the builder.</param>
    /// <param name="operation">The operation to run against the scoped generator.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The tracked result returned by <paramref name="operation"/>.</returns>
    Task<AITrackedImageResult<TResult>> InvokeWithTrackingAsync<TResult>(
        Action<AIImageGenerationBuilder> configure,
        Func<IImageGenerator, CancellationToken, Task<AITrackedImageResult<TResult>>> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the image-generation models available for the resolved profile, with per-model size-constraint
    /// metadata, plus the model the profile is bound to — for up-front validation.
    /// </summary>
    /// <param name="configure">Action to configure the inline image generation via the builder.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The supported models and the resolved bound model ID.</returns>
    Task<AISupportedImageModels> GetSupportedModelsAsync(
        Action<AIImageGenerationBuilder> configure,
        CancellationToken cancellationToken = default);
}
