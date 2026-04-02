using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.RuntimeContext;

#pragma warning disable MEAI001 // ISpeechToTextClient is experimental in M.E.AI

namespace Umbraco.AI.Core.SpeechToText;

/// <summary>
/// A speech-to-text client decorator that sets profile metadata in the runtime context per-execution.
/// </summary>
/// <remarks>
/// <para>
/// This client wraps a base <see cref="ISpeechToTextClient"/> and automatically populates runtime context
/// with profile metadata (ProfileId, ProfileAlias, ProviderId, ModelId) whenever a transcription operation
/// is executed.
/// </para>
/// <para>
/// <strong>Scope Management:</strong>
/// </para>
/// <list type="bullet">
///   <item>If an active scope exists, uses it to set metadata</item>
///   <item>If no scope exists, creates a temporary scope for the execution</item>
///   <item>Automatically disposes any scope it creates after execution completes</item>
/// </list>
/// </remarks>
internal sealed class ScopedProfileSpeechToTextClient : AIBoundSpeechToTextClientBase
{
    private readonly AIProfile _profile;
    private readonly IAIRuntimeContextAccessor _contextAccessor;
    private readonly IAIRuntimeContextScopeProvider _scopeProvider;
    private readonly AIRuntimeContextContributorCollection _contributors;

    public ScopedProfileSpeechToTextClient(
        ISpeechToTextClient innerClient,
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
    public override async Task<SpeechToTextResponse> GetTextAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var scopeExisted = _contextAccessor.Context != null;
        IAIRuntimeContextScope? createdScope = null;

        try
        {
            if (!scopeExisted)
            {
                createdScope = _scopeProvider.CreateScope([]);
                _contributors.Populate(createdScope.Context);
            }

            PopulateProfileMetadata();
            return await base.GetTextAsync(audioSpeechStream, options, cancellationToken);
        }
        finally
        {
            createdScope?.Dispose();
        }
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<SpeechToTextResponseUpdate> GetStreamingTextAsync(
        Stream audioSpeechStream,
        SpeechToTextOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var scopeExisted = _contextAccessor.Context != null;
        IAIRuntimeContextScope? createdScope = null;

        try
        {
            if (!scopeExisted)
            {
                createdScope = _scopeProvider.CreateScope([]);
                _contributors.Populate(createdScope.Context);
            }

            PopulateProfileMetadata();

            await foreach (var update in base.GetStreamingTextAsync(audioSpeechStream, options, cancellationToken))
            {
                yield return update;
            }
        }
        finally
        {
            createdScope?.Dispose();
        }
    }

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
