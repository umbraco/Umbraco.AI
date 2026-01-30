using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Profiles;
using Umbraco.Ai.Core.RuntimeContext;

namespace Umbraco.Ai.Core.Chat;

/// <summary>
/// A chat client decorator that sets profile metadata in the runtime context per-execution.
/// </summary>
/// <remarks>
/// <para>
/// This client wraps a base <see cref="IChatClient"/> and automatically populates runtime context
/// with profile metadata (ProfileId, ProfileAlias, ProviderId, ModelId) whenever a chat operation
/// is executed. The metadata is only set if an active runtime context scope exists.
/// </para>
/// <para>
/// This decorator is used by <see cref="IAiChatClientFactory"/> to ensure profile metadata is
/// available in the runtime context for middleware, logging, and telemetry purposes.
/// </para>
/// <para>
/// <strong>Scope Management:</strong>
/// </para>
/// <list type="bullet">
///   <item>Does NOT create its own scope - expects an active scope to exist</item>
///   <item>If no scope exists, silently skips metadata setting and proceeds with execution</item>
///   <item>Sets metadata at the beginning of each GetResponseAsync/GetStreamingResponseAsync call</item>
///   <item>Works seamlessly with <c>ScopedAIAgent</c> which creates the scope</item>
/// </list>
/// </remarks>
internal sealed class ScopedProfileChatClient : DelegatingChatClient
{
    private readonly AiProfile _profile;
    private readonly IAiRuntimeContextAccessor _contextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScopedProfileChatClient"/> class.
    /// </summary>
    /// <param name="innerClient">The base chat client to wrap.</param>
    /// <param name="profile">The profile containing metadata to set in the runtime context.</param>
    /// <param name="contextAccessor">Accessor for the runtime context.</param>
    public ScopedProfileChatClient(
        IChatClient innerClient,
        AiProfile profile,
        IAiRuntimeContextAccessor contextAccessor)
        : base(innerClient)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        PopulateProfileMetadata();
        return await base.GetResponseAsync(messages, options, cancellationToken);
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        PopulateProfileMetadata();
        await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            yield return update;
        }
    }

    /// <summary>
    /// Populates profile metadata in the runtime context if an active scope exists.
    /// </summary>
    private void PopulateProfileMetadata()
    {
        // Only set metadata if there's an active scope
        if (_contextAccessor.Context is null)
        {
            return;
        }

        _contextAccessor.Context.SetValue(Constants.ContextKeys.ProfileId, _profile.Id);
        _contextAccessor.Context.SetValue(Constants.ContextKeys.ProfileAlias, _profile.Alias);
        _contextAccessor.Context.SetValue(Constants.ContextKeys.ProfileVersion, _profile.Version);
        _contextAccessor.Context.SetValue(Constants.ContextKeys.ProviderId, _profile.Model.ProviderId);
        _contextAccessor.Context.SetValue(Constants.ContextKeys.ModelId, _profile.Model.ModelId);
    }
}
