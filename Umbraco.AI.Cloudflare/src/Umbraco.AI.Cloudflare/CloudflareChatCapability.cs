using Microsoft.Extensions.AI;
using OpenAI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Cloudflare;

/// <summary>
/// AI chat capability for Cloudflare Workers AI.
/// </summary>
/// <remarks>
/// Chats through Workers AI's OpenAI-compatible Chat Completions route
/// (<c>{Endpoint}/accounts/{AccountId}/ai/v1</c>) using the official OpenAI client, and lists models from
/// Cloudflare's model search. Cloudflare has no <c>/models</c> route on its OpenAI-compatible API, so the stock
/// OpenAI provider can't populate a model list against it.
/// </remarks>
public class CloudflareChatCapability(CloudflareProvider provider)
    : AIChatCapabilityBase<CloudflareProviderSettings>(provider)
{
    private const string DefaultChatModel = "@cf/meta/llama-3.3-70b-instruct-fp8-fast";

    private new CloudflareProvider Provider => (CloudflareProvider)base.Provider;

    /// <inheritdoc />
    /// <remarks>
    /// Lists the account's Text Generation models. A failed lookup (wrong token, wrong account) throws with
    /// Cloudflare's own message, so "Test connection" reports why.
    /// </remarks>
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        CloudflareProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var modelIds = await Provider.GetChatModelIdsAsync(settings, cancellationToken);

        return modelIds
            .Select(id => new AIModelDescriptor(new AIModelRef(Provider.Id, id), CloudflareModelNames.FormatDisplayName(id)))
            .ToList();
    }

    /// <inheritdoc />
    protected override Task<IChatClient> CreateClientAsync(
        CloudflareProviderSettings settings,
        string? modelId,
        CancellationToken cancellationToken = default)
    {
        CloudflareApi.ValidateSettings(settings);

        IChatClient client = CloudflareProvider.CreateOpenAIClient(settings)
            .GetChatClient(modelId ?? DefaultChatModel)
            .AsIChatClient();

        return Task.FromResult(client);
    }
}
