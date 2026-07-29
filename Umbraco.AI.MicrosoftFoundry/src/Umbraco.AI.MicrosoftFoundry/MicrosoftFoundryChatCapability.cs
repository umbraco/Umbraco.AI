using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.MicrosoftFoundry;

/// <summary>
/// AI chat capability for Microsoft AI Foundry provider.
/// </summary>
/// <remarks>
/// Supports all chat models available through Microsoft AI Foundry, including
/// OpenAI (GPT-4, GPT-4o), Mistral, Llama, Cohere, Phi, and more.
/// </remarks>
public class MicrosoftFoundryChatCapability(MicrosoftFoundryProvider provider, ILogger<MicrosoftFoundryChatCapability> logger) : AIChatCapabilityBase<MicrosoftFoundryProviderSettings>(provider)
{
    private const string DefaultChatModel = "gpt-4o";

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
            .Where(IsChatModel)
            .Select(m => new AIModelDescriptor(
                new AIModelRef(Provider.Id, m.Id),
                MicrosoftFoundryModelUtilities.FormatDisplayName(m.Id, m.ModelName, m.ModelVersion),
                BuildSettingsSupport(m.Id, m).ToMetadata()))
            .ToList();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Foundry fronts other vendors' models, so it inherits their restrictions: an o-series or GPT-5
    /// deployment rejects the sampling parameters exactly as it would on OpenAI directly, and a Claude 4.7+
    /// deployment as it would on Anthropic. The base enforces whatever this returns, so the model list, the
    /// editor and the request all read one declaration.
    /// </remarks>
    public override AIModelSettingsSupport GetSettingsSupport(string modelId)
        => BuildSettingsSupport(modelId, Provider.TryGetModelInfo(modelId));

    /// <summary>
    /// Turns a model's listing entry into the settings declaration the editor reads and the base enforces.
    /// </summary>
    /// <remarks>
    /// The entry is what makes the deployments API path accurate: it carries the model a deployment fronts,
    /// so a deployment called <c>prod-chat</c> is judged on the <c>o3</c> behind it. A <c>null</c> entry —
    /// the models API path, or a model absent from the last listing — falls back to reasoning from the ID.
    /// </remarks>
    private static AIModelSettingsSupport BuildSettingsSupport(string modelId, MicrosoftFoundryModelInfo? info)
        => MicrosoftFoundryModelUtilities.SupportsSamplingParameters(modelId, info?.ModelName, info?.ModelPublisher)
            ? AIModelSettingsSupport.Default
            : new AIModelSettingsSupport
            {
                UnsupportedProfileSettings = AIProfileSettingKeys.Sampling,
            };

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Fetches the model list (cached) before building the client, so <see cref="GetSettingsSupport"/> can
    /// read the target deployment's underlying model synchronously when the base enforces the declaration.
    /// Without the prefetch, a deployment name that hides its model would always fall back to the ID.
    /// </para>
    /// <para>
    /// A failure is not fatal — the listing refines the decision but is not required to make a request, and
    /// losing it must not stop chat from working — so it degrades to reasoning from the ID, and logs,
    /// because that fallback is less accurate and otherwise invisible. Cancellation is not caught: if the
    /// caller has given up, this should too rather than continue building a client.
    /// </para>
    /// </remarks>
    [Experimental("OPENAI001")]
    protected override async Task<IChatClient> CreateClientAsync(
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
            logger.LogDebug(
                ex,
                "Could not list Microsoft AI Foundry models while creating a chat client. The model a "
                + "deployment fronts is unavailable, so setting support will be inferred from the model ID "
                + "instead.");
        }

        var model = modelId ?? DefaultChatModel;

        // The declaration from GetSettingsSupport is enforced by the base, which wraps this client so the
        // sampling parameters are stripped for a model that rejects them. See DeclaredSettingsChatClient.
        if (settings.UseResponsesApi)
        {
            return MicrosoftFoundryProvider.CreateOpenAIClient(settings, logger)
                .GetResponsesClient()
                .AsIChatClient(model);
        }

        return MicrosoftFoundryProvider.CreateAzureOpenAIClient(settings)
            .GetChatClient(model)
            .AsIChatClient();
    }

    private static bool IsChatModel(MicrosoftFoundryModelInfo model)
    {
        // If capabilities are provided, use them
        if (model.Capabilities is not null)
        {
            return model.Capabilities.ChatCompletion;
        }

        // Fallback: exclude known embedding model patterns
        var id = model.Id.ToLowerInvariant();
        return !id.Contains("embedding") && !id.Contains("embed");
    }
}
