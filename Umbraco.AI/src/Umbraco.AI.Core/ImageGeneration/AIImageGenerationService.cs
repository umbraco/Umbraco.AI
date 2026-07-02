using System.Diagnostics;
using System.Drawing;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Extensions;
using Umbraco.Cms.Core.Events;

#pragma warning disable MEAI001 // IImageGenerator and image types are experimental in M.E.AI
#pragma warning disable UMBRACOAI_IMAGEGEN // Implements the experimental image-generation API

namespace Umbraco.AI.Core.ImageGeneration;

internal sealed class AIImageGenerationService : IAIImageGenerationService
{
    private readonly IAIImageGeneratorFactory _generatorFactory;
    private readonly IAIProfileService _profileService;
    private readonly IAIGuardrailService _guardrailService;
    private readonly IAIConnectionService _connectionService;
    private readonly IEventAggregator _eventAggregator;
    private readonly IAIRuntimeContextAccessor _contextAccessor;
    private readonly IAIRuntimeContextScopeProvider _scopeProvider;
    private readonly AIRuntimeContextContributorCollection _contributors;
    private readonly AIImageGenerationTracker _tracker;

    public AIImageGenerationService(
        IAIImageGeneratorFactory generatorFactory,
        IAIProfileService profileService,
        IAIGuardrailService guardrailService,
        IAIConnectionService connectionService,
        IEventAggregator eventAggregator,
        IAIRuntimeContextAccessor contextAccessor,
        IAIRuntimeContextScopeProvider scopeProvider,
        AIRuntimeContextContributorCollection contributors,
        AIImageGenerationTracker tracker)
    {
        _generatorFactory = generatorFactory;
        _profileService = profileService;
        _guardrailService = guardrailService;
        _connectionService = connectionService;
        _eventAggregator = eventAggregator;
        _contextAccessor = contextAccessor;
        _scopeProvider = scopeProvider;
        _contributors = contributors;
        _tracker = tracker;
    }

    public Task<ImageGenerationResponse> GenerateImagesAsync(
        Action<AIImageGenerationBuilder> configure,
        string prompt,
        CancellationToken cancellationToken = default)
        => GenerateImagesAsync(configure, prompt, originalImages: null, cancellationToken);

    public async Task<ImageGenerationResponse> GenerateImagesAsync(
        Action<AIImageGenerationBuilder> configure,
        string prompt,
        IEnumerable<AIContent>? originalImages,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        var builder = BuildGeneration(configure);

        // Pass-through mode: skip notifications and duration tracking.
        // The parent feature handles its own observability.
        if (builder.IsPassThrough)
        {
            return await ExecuteGenerationAsync(builder, prompt, originalImages, cancellationToken);
        }

        // Publish executing notification
        var eventMessages = new EventMessages();
        var executingNotification = new AIImageGenerationExecutingNotification(
            builder.Id, builder.Alias!, builder.Name, builder.ProfileId, eventMessages);
        await _eventAggregator.PublishAsync(executingNotification, cancellationToken);

        if (executingNotification.Cancel)
        {
            var errorMessages = string.Join("; ", eventMessages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Inline image generation cancelled: {errorMessages}");
        }

        var stopwatch = Stopwatch.StartNew();
        bool isSuccess = false;

        try
        {
            var response = await ExecuteGenerationAsync(builder, prompt, originalImages, cancellationToken);
            isSuccess = true;
            return response;
        }
        finally
        {
            var executedNotification = new AIImageGenerationExecutedNotification(
                builder.Id, builder.Alias!, builder.Name, builder.ProfileId,
                stopwatch.Elapsed, isSuccess, eventMessages);
            await _eventAggregator.PublishAsync(executedNotification, cancellationToken);
        }
    }

    public async Task<IImageGenerator> CreateImageGeneratorAsync(
        Action<AIImageGenerationBuilder> configure,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = BuildGeneration(configure);

        await ResolveBuilderAliasesAsync(builder, cancellationToken);
        var profile = await ResolveProfileAsync(builder.ProfileId, builder.ProfileAlias, cancellationToken);

        var generator = await _generatorFactory.CreateGeneratorAsync(profile, cancellationToken);

        return new ScopedInlineImageGenerator(generator, builder, _contextAccessor, _scopeProvider, _contributors);
    }

    public async Task<AITrackedImageResult<TResult>> InvokeWithTrackingAsync<TResult>(
        Action<AIImageGenerationBuilder> configure,
        Func<IImageGenerator, CancellationToken, Task<AITrackedImageResult<TResult>>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(operation);

        var builder = BuildGeneration(configure);

        var scopeExisted = _contextAccessor.Context is not null;
        IAIRuntimeContextScope? createdScope = null;

        try
        {
            if (!scopeExisted)
            {
                createdScope = _scopeProvider.CreateScope(builder.ContextItems ?? []);
                _contributors.Populate(createdScope.Context);
            }

            await ResolveBuilderAliasesAsync(builder, cancellationToken);

            var context = _contextAccessor.Context!;
            builder.PopulateContext(context, setFeatureMetadata: !scopeExisted);

            var profile = await ResolveProfileAsync(builder.ProfileId, builder.ProfileAlias, cancellationToken);

            // The raw escape-hatch call bypasses the scoped generator's GenerateAsync (which is what
            // normally writes profile metadata), so populate it here for the usage/audit records.
            PopulateProfileMetadata(context, profile);

            var generator = await _generatorFactory.CreateGeneratorAsync(profile, cancellationToken);

            // Record usage + audit via the same tracker the middleware uses, so the raw call stays
            // visible in analytics/audit even though it bypasses the GenerateAsync pipeline.
            var promptData = $"image-generation (tracked): {builder.Alias}";
            return await _tracker.TrackAsync(promptData, token => operation(generator, token), cancellationToken);
        }
        finally
        {
            createdScope?.Dispose();
        }
    }

    public async Task<AISupportedImageModels> GetSupportedModelsAsync(
        Action<AIImageGenerationBuilder> configure,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);

        // This is a read-only metadata lookup — it only uses the profile, never the alias (no ID
        // generation, notifications, telemetry or audit), so an alias isn't required here.
        var builder = BuildGeneration(configure, validate: false);
        var profile = await ResolveProfileAsync(builder.ProfileId, builder.ProfileAlias, cancellationToken);

        var configured = await _connectionService.GetConfiguredProviderAsync(profile.ConnectionId, cancellationToken);
        var capability = configured?.GetCapability<IAIConfiguredImageGeneratorCapability>();
        if (capability is null)
        {
            throw new InvalidOperationException(
                $"Provider '{profile.Model.ProviderId}' does not support image-generation capability.");
        }

        var models = await capability.GetModelsAsync(cancellationToken);

        return new AISupportedImageModels
        {
            Models = models,
            ModelId = profile.Model.ModelId,
        };
    }

    private async Task<ImageGenerationResponse> ExecuteGenerationAsync(
        AIImageGenerationBuilder builder,
        string prompt,
        IEnumerable<AIContent>? originalImages,
        CancellationToken cancellationToken)
    {
        var scopeExisted = _contextAccessor.Context is not null;
        IAIRuntimeContextScope? createdScope = null;

        try
        {
            if (!scopeExisted)
            {
                createdScope = _scopeProvider.CreateScope(builder.ContextItems ?? []);
                _contributors.Populate(createdScope.Context);
            }

            await ResolveBuilderAliasesAsync(builder, cancellationToken);
            builder.PopulateContext(_contextAccessor.Context!, setFeatureMetadata: !builder.IsPassThrough);

            var profile = await ResolveProfileAsync(builder.ProfileId, builder.ProfileAlias, cancellationToken);
            var generator = await _generatorFactory.CreateGeneratorAsync(profile, cancellationToken);
            var mergedOptions = MergeOptions(profile, builder.ImageGenerationOptions);

            var effectiveOriginalImages = originalImages ?? builder.OriginalImages;
            var request = effectiveOriginalImages is not null
                ? new ImageGenerationRequest(prompt, effectiveOriginalImages)
                : new ImageGenerationRequest(prompt);

            return await generator.GenerateAsync(request, mergedOptions, cancellationToken);
        }
        finally
        {
            createdScope?.Dispose();
        }
    }

    private static AIImageGenerationBuilder BuildGeneration(Action<AIImageGenerationBuilder> configure, bool validate = true)
    {
        var builder = new AIImageGenerationBuilder();
        configure(builder);
        if (validate)
        {
            builder.Validate();
        }

        return builder;
    }

    private async Task<AIProfile> ResolveProfileAsync(Guid? profileId, string? profileAlias, CancellationToken cancellationToken)
    {
        if (!profileId.HasValue && !string.IsNullOrWhiteSpace(profileAlias))
        {
            profileId = await _profileService.GetProfileIdByAliasAsync(profileAlias, cancellationToken);
        }

        var profile = profileId.HasValue
            ? await _profileService.GetProfileAsync(profileId.Value, cancellationToken)
            : await _profileService.GetDefaultProfileAsync(AICapability.ImageGeneration, cancellationToken);

        if (profile is null)
        {
            throw new InvalidOperationException($"AI profile with ID '{profileId}' not found.");
        }

        EnsureProfileSupportsImageGeneration(profile);
        return profile;
    }

    private async Task ResolveBuilderAliasesAsync(AIImageGenerationBuilder builder, CancellationToken cancellationToken)
    {
        if (builder.GuardrailAliases is { Count: > 0 } aliases)
        {
            builder.SetResolvedGuardrailIds(
                await _guardrailService.GetGuardrailIdsByAliasesAsync(aliases, cancellationToken));
        }

        if (builder.AdditionalGuardrailAliases is { Count: > 0 } additionalAliases)
        {
            builder.SetResolvedAdditionalGuardrailIds(
                await _guardrailService.GetGuardrailIdsByAliasesAsync(additionalAliases, cancellationToken));
        }
    }

    private static void PopulateProfileMetadata(AIRuntimeContext context, AIProfile profile)
    {
        context.SetValue(Constants.ContextKeys.ProfileId, profile.Id);
        context.SetValue(Constants.ContextKeys.ProfileAlias, profile.Alias);
        context.SetValue(Constants.ContextKeys.ProfileVersion, profile.Version);
        context.SetValue(Constants.ContextKeys.ProviderId, profile.Model.ProviderId);
        context.SetValue(Constants.ContextKeys.ModelId, profile.Model.ModelId);
    }

    private static ImageGenerationOptions MergeOptions(AIProfile profile, ImageGenerationOptions? callerOptions)
    {
        var settings = profile.Settings as AIImageGenerationProfileSettings;
        var options = callerOptions?.Clone() ?? new ImageGenerationOptions();

        // Profile carries use-case policy defaults; caller options (Count, ResponseFormat, etc.) win.
        options.ModelId ??= profile.Model.ModelId;
        options.ImageSize ??= ParseSize(settings?.Size);

        if (string.IsNullOrWhiteSpace(options.MediaType) && !string.IsNullOrWhiteSpace(settings?.MediaType))
        {
            options.MediaType = settings.MediaType;
        }

        // Forward provider-specific hints for adapters that read them.
        if (!string.IsNullOrWhiteSpace(settings?.Quality))
        {
            options.AdditionalProperties ??= new AdditionalPropertiesDictionary();
            options.AdditionalProperties["quality"] = settings.Quality;
        }

        if (!string.IsNullOrWhiteSpace(settings?.Style))
        {
            options.AdditionalProperties ??= new AdditionalPropertiesDictionary();
            options.AdditionalProperties["style"] = settings.Style;
        }

        return options;
    }

    private static Size? ParseSize(string? size)
    {
        if (string.IsNullOrWhiteSpace(size))
        {
            return null;
        }

        var parts = size.Split('x', 'X', '×');
        if (parts.Length == 2
            && int.TryParse(parts[0].Trim(), out var width)
            && int.TryParse(parts[1].Trim(), out var height))
        {
            return new Size(width, height);
        }

        return null;
    }

    private static void EnsureProfileSupportsImageGeneration(AIProfile profile)
    {
        if (profile.Capability != AICapability.ImageGeneration)
        {
            throw new InvalidOperationException($"The profile '{profile.Name}' does not support image-generation capability.");
        }
    }
}
