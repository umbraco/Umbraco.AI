using System.Diagnostics.CodeAnalysis;
using Umbraco.AI.Core.Providers.Errors;

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// Defines an AI decision service that resolves a Decision profile (by ID or alias) and answers a
/// typed <see cref="AIDecisionQuestion{TResponse}"/> through it. This service acts as a thin layer over
/// <see cref="IAIDecisionClientFactory"/>, adding Umbraco-specific profile/connection resolution —
/// mirrors <see cref="Umbraco.AI.Core.SpeechToText.IAISpeechToTextService"/>'s profile-resolving shape.
/// </summary>
/// <remarks>
/// Unlike an HTTP request DTO, none of these methods take a single "ID or alias" parameter type —
/// Core services never do (see <see cref="Umbraco.AI.Core.Chat.IAIChatService"/>/
/// <see cref="Umbraco.AI.Core.SpeechToText.IAISpeechToTextService"/>, whose non-obsolete surface is the
/// builder pattern with distinct <c>WithProfile(Guid)</c>/<c>WithProfile(string)</c> methods). The
/// profile-typed and no-profile overloads below are thin convenience wrappers over the builder-based
/// <see cref="AskAsync{TResponse}(Action{AIDecisionBuilder}, AIDecisionQuestion{TResponse}, CancellationToken)"/>
/// overload — Decision has no shipped legacy surface to preserve, so none of them needs an
/// <see cref="ObsoleteAttribute"/> the way Chat's/SpeechToText's equivalents do.
/// </remarks>
/// <remarks>
/// Every overload is generic on <c>TResponse</c>: the concrete
/// <see cref="AIDecisionQuestion{TResponse}"/> subclass a caller passes (<see cref="AIBinaryDecisionQuestion"/>,
/// <see cref="AIChoiceDecisionQuestion"/>, <see cref="AIScoreDecisionQuestion"/>) fixes
/// <c>TResponse</c>, so the caller gets its matching response type back with no cast.
/// <see cref="IAIDecisionClient"/> underneath stays non-generic — it returns the base
/// <see cref="AIDecisionResponse"/> — so the primary check that a provider answered the wrong question
/// shape already happens inside <see cref="AIErrorClassifyingDecisionClient"/>, which sits inside the
/// tracking middleware so the mismatch is recorded as a tracked/audited failure rather than a false
/// success. This service only keeps a narrowing guard afterwards as defence in depth, and throws
/// <see cref="AIProviderException"/> if it ever trips (see each overload's <c>AIProviderException</c>
/// remarks).
/// </remarks>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public interface IAIDecisionService
{
    /// <summary>
    /// Asks the given question against the default Decision profile.
    /// </summary>
    /// <typeparam name="TResponse">The response type the question resolves to.</typeparam>
    /// <param name="question">The question to ask.</param>
    /// <param name="options">Optional per-call overrides (e.g. model).</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The typed decision response.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="question"/> is shaped incorrectly for its concrete type (e.g. blank instructions,
    /// or an <see cref="AIChoiceDecisionQuestion"/> with fewer than two options). Rejected before any
    /// provider is reached — see <see cref="ValidatingDecisionClient"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// No default Decision profile is configured (neither stored nor via
    /// <c>AIOptions.DefaultDecisionProfileAlias</c>).
    /// </exception>
    /// <exception cref="AIProviderException">
    /// The provider returned a response whose runtime type doesn't match <typeparamref name="TResponse"/>
    /// — a provider bug, not a caller error (see the remarks on this interface).
    /// </exception>
    Task<TResponse> AskAsync<TResponse>(
        AIDecisionQuestion<TResponse> question,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default)
        where TResponse : AIDecisionResponse;

    /// <summary>
    /// Resolves the given profile by ID and asks it the supplied <paramref name="question"/>.
    /// </summary>
    /// <typeparam name="TResponse">The response type the question resolves to.</typeparam>
    /// <param name="profileId">The ID of the Decision profile to use.</param>
    /// <param name="question">The question to ask.</param>
    /// <param name="options">Optional per-call overrides (e.g. model).</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The typed decision response.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="question"/> is shaped incorrectly for its concrete type (e.g. blank instructions,
    /// or an <see cref="AIChoiceDecisionQuestion"/> with fewer than two options). Rejected before any
    /// provider is reached — see <see cref="ValidatingDecisionClient"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// No profile matches <paramref name="profileId"/>, or it isn't a Decision profile.
    /// </exception>
    /// <exception cref="AIProviderException">
    /// The provider returned a response whose runtime type doesn't match <typeparamref name="TResponse"/>
    /// — a provider bug, not a caller error (see the remarks on this interface).
    /// </exception>
    Task<TResponse> AskAsync<TResponse>(
        Guid profileId,
        AIDecisionQuestion<TResponse> question,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default)
        where TResponse : AIDecisionResponse;

    /// <summary>
    /// Resolves the given profile by alias and asks it the supplied <paramref name="question"/>.
    /// </summary>
    /// <typeparam name="TResponse">The response type the question resolves to.</typeparam>
    /// <param name="profileAlias">The alias of the Decision profile to use.</param>
    /// <param name="question">The question to ask.</param>
    /// <param name="options">Optional per-call overrides (e.g. model).</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The typed decision response.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="question"/> is shaped incorrectly for its concrete type (e.g. blank instructions,
    /// or an <see cref="AIChoiceDecisionQuestion"/> with fewer than two options). Rejected before any
    /// provider is reached — see <see cref="ValidatingDecisionClient"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// No profile matches <paramref name="profileAlias"/>, or it isn't a Decision profile.
    /// </exception>
    /// <exception cref="AIProviderException">
    /// The provider returned a response whose runtime type doesn't match <typeparamref name="TResponse"/>
    /// — a provider bug, not a caller error (see the remarks on this interface).
    /// </exception>
    Task<TResponse> AskAsync<TResponse>(
        string profileAlias,
        AIDecisionQuestion<TResponse> question,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default)
        where TResponse : AIDecisionResponse;

    /// <summary>
    /// Asks a decision using an inline decision builder, with full observability
    /// (notifications, telemetry, duration tracking).
    /// </summary>
    /// <remarks>
    /// This is the primary entry point — the no-profile, <c>Guid</c>, and <c>string</c> overloads all
    /// delegate to this overload internally, the same way Chat's/SpeechToText's profile-id/alias
    /// overloads delegate to their own builder-based main path.
    /// </remarks>
    /// <typeparam name="TResponse">The response type the question resolves to.</typeparam>
    /// <param name="configure">Action to configure the inline decision via the builder.</param>
    /// <param name="question">The question to ask.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The typed decision response.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="question"/> is shaped incorrectly for its concrete type.
    /// Rejected before any provider is reached — see <see cref="ValidatingDecisionClient"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// No profile matches the builder's configured profile ID/alias (or none was configured and no
    /// default Decision profile exists), the resolved profile isn't a Decision profile, or a subscriber
    /// cancelled the execution via <see cref="AIDecisionExecutingNotification"/>.
    /// </exception>
    /// <exception cref="AIProviderException">
    /// The provider returned a response whose runtime type doesn't match <typeparamref name="TResponse"/>
    /// — a provider bug, not a caller error (see the remarks on this interface).
    /// </exception>
    Task<TResponse> AskAsync<TResponse>(
        Action<AIDecisionBuilder> configure,
        AIDecisionQuestion<TResponse> question,
        CancellationToken cancellationToken = default)
        where TResponse : AIDecisionResponse;
}
