using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.RuntimeContext;

#pragma warning disable MEAI001 // ISpeechToTextClient is experimental in M.E.AI

namespace Umbraco.AI.Core.SpeechToText;

/// <summary>
/// A speech-to-text client decorator that manages runtime context scope per-execution
/// and sets inline speech-to-text metadata in the runtime context.
/// </summary>
/// <remarks>
/// <para>
/// Each call to <see cref="GetTextAsync"/> or <see cref="GetStreamingTextAsync"/>
/// ensures a scope exists, populates it via contributors if newly created, sets inline
/// speech-to-text feature metadata (only when no parent scope already set it), delegates
/// to the inner client, and disposes any scope it created. This mirrors the
/// <c>ScopedProfileSpeechToTextClient</c> pattern.
/// </para>
/// <para>
/// This client is returned by <see cref="IAISpeechToTextService.CreateSpeechToTextClientAsync"/>
/// and does not publish notifications.
/// </para>
/// </remarks>
internal sealed class ScopedInlineSpeechToTextClient : AIBoundSpeechToTextClientBase
{
    private readonly AISpeechToTextBuilder _builder;
    private readonly IAIRuntimeContextAccessor _contextAccessor;
    private readonly IAIRuntimeContextScopeProvider _scopeProvider;
    private readonly AIRuntimeContextContributorCollection _contributors;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScopedInlineSpeechToTextClient"/> class.
    /// </summary>
    /// <param name="innerClient">The base speech-to-text client to delegate to.</param>
    /// <param name="builder">The inline speech-to-text builder containing configuration.</param>
    /// <param name="contextAccessor">Accessor for the runtime context.</param>
    /// <param name="scopeProvider">Provider for creating runtime context scopes.</param>
    /// <param name="contributors">Collection of context contributors to populate the scope.</param>
    internal ScopedInlineSpeechToTextClient(
        ISpeechToTextClient innerClient,
        AISpeechToTextBuilder builder,
        IAIRuntimeContextAccessor contextAccessor,
        IAIRuntimeContextScopeProvider scopeProvider,
        AIRuntimeContextContributorCollection contributors)
        : base(innerClient)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
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
        var scopeExisted = _contextAccessor.Context is not null;
        IAIRuntimeContextScope? createdScope = null;

        try
        {
            if (!scopeExisted)
            {
                createdScope = _scopeProvider.CreateScope(_builder.ContextItems ?? []);
                _contributors.Populate(createdScope.Context);
            }

            _builder.PopulateContext(_contextAccessor.Context!, setFeatureMetadata: !scopeExisted);
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
        var scopeExisted = _contextAccessor.Context is not null;
        IAIRuntimeContextScope? createdScope = null;

        try
        {
            if (!scopeExisted)
            {
                createdScope = _scopeProvider.CreateScope(_builder.ContextItems ?? []);
                _contributors.Populate(createdScope.Context);
            }

            _builder.PopulateContext(_contextAccessor.Context!, setFeatureMetadata: !scopeExisted);

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
}
