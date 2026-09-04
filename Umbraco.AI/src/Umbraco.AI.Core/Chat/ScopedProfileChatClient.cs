using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Core.Chat;

/// <summary>
/// A chat client decorator that sets profile metadata in the runtime context per-execution.
/// </summary>
/// <remarks>
/// <para>
/// This client wraps a base <see cref="IChatClient"/> and automatically populates runtime context
/// with profile metadata (ProfileId, ProfileAlias, ProviderId, ModelId) whenever a chat operation
/// is executed.
/// </para>
/// <para>
/// This decorator is used by <see cref="IAIChatClientFactory"/> to ensure profile metadata is
/// available in the runtime context for middleware, logging, and telemetry purposes.
/// </para>
/// <para>
/// <strong>Scope Management:</strong>
/// </para>
/// <list type="bullet">
///   <item>If an active scope exists, uses it to set metadata (e.g., when used with <c>ScopedAIAgent</c>)</item>
///   <item>If no scope exists, creates a temporary scope for the execution</item>
///   <item>Automatically disposes any scope it creates after execution completes</item>
///   <item>Works standalone or as part of a scoped agent</item>
/// </list>
/// </remarks>
internal sealed class ScopedProfileChatClient : DelegatingChatClient
{
    private readonly AIProfile _profile;
    private readonly IAIRuntimeContextAccessor _contextAccessor;
    private readonly IAIRuntimeContextScopeProvider _scopeProvider;
    private readonly AIRuntimeContextContributorCollection _contributors;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScopedProfileChatClient"/> class.
    /// </summary>
    /// <param name="innerClient">The base chat client to wrap.</param>
    /// <param name="profile">The profile containing metadata to set in the runtime context.</param>
    /// <param name="contextAccessor">Accessor for the runtime context.</param>
    /// <param name="scopeProvider">Provider for creating runtime context scopes.</param>
    /// <param name="contributors">Collection of context contributors to populate the scope.</param>
    public ScopedProfileChatClient(
        IChatClient innerClient,
        AIProfile profile,
        IAIRuntimeContextAccessor contextAccessor,
        IAIRuntimeContextScopeProvider scopeProvider,
        AIRuntimeContextContributorCollection contributors)
        : base(innerClient)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
        _contributors = contributors ?? throw new ArgumentNullException(nameof(contributors));
    }

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var scopeExisted = _contextAccessor.Context != null;
        IAIRuntimeContextScope? createdScope = null;

        try
        {
            if (!scopeExisted)
            {
                // Create temporary scope for this execution
                createdScope = _scopeProvider.CreateScope([]);
                _contributors.Populate(createdScope.Context);
            }

            PopulateProfileMetadata();
            return await base.GetResponseAsync(messages, options, cancellationToken);
        }
        finally
        {
            // Only dispose scope we created
            createdScope?.Dispose();
        }
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var scopeExisted = _contextAccessor.Context != null;
        IAIRuntimeContextScope? createdScope = null;

        try
        {
            if (!scopeExisted)
            {
                // Create temporary scope for this execution
                createdScope = _scopeProvider.CreateScope([]);
                _contributors.Populate(createdScope.Context);
            }

            PopulateProfileMetadata();

            await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
            {
                yield return update;
            }
        }
        finally
        {
            // Only dispose scope we created
            createdScope?.Dispose();
        }
    }

    /// <summary>
    /// Populates profile metadata in the runtime context.
    /// </summary>
    /// <remarks>
    /// This method assumes a scope exists (either pre-existing or just created).
    /// </remarks>
    private void PopulateProfileMetadata()
    {
        var context = _contextAccessor.Context;
        if (context is null)
        {
            return;
        }

        context.SetValue(Constants.ContextKeys.ProfileId, _profile.Id);
        context.SetValue(Constants.ContextKeys.ProfileAlias, _profile.Alias);
        context.SetValue(Constants.ContextKeys.ProfileVersion, _profile.Version);
        context.SetValue(Constants.ContextKeys.ProviderId, _profile.Model.ProviderId);
        context.SetValue(Constants.ContextKeys.ModelId, _profile.Model.ModelId);
    }
}
