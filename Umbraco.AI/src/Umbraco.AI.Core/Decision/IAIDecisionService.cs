using System.Diagnostics.CodeAnalysis;
using Umbraco.AI.Core.Providers.Errors;

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// Defines an AI decision service that resolves a Decision profile (by ID or alias) and answers one or
/// more typed <see cref="AIDecisionQuestion"/>s through it. This service acts as a thin layer over
/// <see cref="IAIDecisionClientFactory"/>, adding Umbraco-specific profile/connection resolution —
/// mirrors <see cref="Umbraco.AI.Core.SpeechToText.IAISpeechToTextService"/>'s profile-resolving shape.
/// </summary>
/// <remarks>
/// Unlike an HTTP request DTO, none of these methods take a single "ID or alias" parameter type —
/// Core services never do (see <see cref="Umbraco.AI.Core.Chat.IAIChatService"/>/
/// <see cref="Umbraco.AI.Core.SpeechToText.IAISpeechToTextService"/>, whose non-obsolete surface is the
/// builder pattern with distinct <c>WithProfile(Guid)</c>/<c>WithProfile(string)</c> methods). The
/// profile-typed and no-profile overloads below are thin convenience wrappers over the builder-based
/// main overload of each method — Decision has no shipped legacy surface to preserve, so none of them
/// needs an <see cref="ObsoleteAttribute"/> the way Chat's/SpeechToText's equivalents do.
/// </remarks>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public interface IAIDecisionService
{
    /// <summary>
    /// Asks a single question against the default Decision profile.
    /// </summary>
    /// <remarks>
    /// A thin helper over <see cref="GetDecisionResponseAsync(AIDecisionRequest, AIDecisionOptions?, CancellationToken)"/>:
    /// it builds a one-question <see cref="AIDecisionRequest"/> (assigning an internal id when
    /// <paramref name="question"/>'s <see cref="AIDecisionQuestion.Id"/> is null), asks it through the
    /// same pipeline, and unwraps the single answer. The primary check that a provider answered the
    /// wrong shape already happens inside <see cref="AIErrorClassifyingDecisionClient"/>, which sits
    /// inside the tracking middleware so the mismatch is recorded as a tracked/audited failure rather
    /// than a false success — this method only keeps a narrowing guard afterwards as defence in depth.
    /// </remarks>
    /// <typeparam name="TAnswer">The answer type the question resolves to.</typeparam>
    /// <param name="question">The question to ask.</param>
    /// <param name="state">The content being judged, when there is any.</param>
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
    /// The provider returned an answer whose runtime type doesn't match <typeparamref name="TAnswer"/>
    /// — a provider bug, not a caller error (see the remarks on this interface).
    /// </exception>
    Task<AIDecisionResponse<TAnswer>> AskAsync<TAnswer>(
        AIDecisionQuestion<TAnswer> question,
        string? state = null,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default)
        where TAnswer : AIDecisionAnswer;

    /// <summary>
    /// Resolves the given profile by ID and asks it a single <paramref name="question"/>.
    /// </summary>
    /// <typeparam name="TAnswer">The answer type the question resolves to.</typeparam>
    /// <param name="profileId">The ID of the Decision profile to use.</param>
    /// <param name="question">The question to ask.</param>
    /// <param name="state">The content being judged, when there is any.</param>
    /// <param name="options">Optional per-call overrides (e.g. model).</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The typed decision response.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="question"/> is shaped incorrectly for its concrete type. Rejected before any
    /// provider is reached — see <see cref="ValidatingDecisionClient"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// No profile matches <paramref name="profileId"/>, or it isn't a Decision profile.
    /// </exception>
    /// <exception cref="AIProviderException">
    /// The provider returned an answer whose runtime type doesn't match <typeparamref name="TAnswer"/>
    /// — a provider bug, not a caller error (see the remarks on this interface).
    /// </exception>
    Task<AIDecisionResponse<TAnswer>> AskAsync<TAnswer>(
        Guid profileId,
        AIDecisionQuestion<TAnswer> question,
        string? state = null,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default)
        where TAnswer : AIDecisionAnswer;

    /// <summary>
    /// Resolves the given profile by alias and asks it a single <paramref name="question"/>.
    /// </summary>
    /// <typeparam name="TAnswer">The answer type the question resolves to.</typeparam>
    /// <param name="profileAlias">The alias of the Decision profile to use.</param>
    /// <param name="question">The question to ask.</param>
    /// <param name="state">The content being judged, when there is any.</param>
    /// <param name="options">Optional per-call overrides (e.g. model).</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The typed decision response.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="question"/> is shaped incorrectly for its concrete type. Rejected before any
    /// provider is reached — see <see cref="ValidatingDecisionClient"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// No profile matches <paramref name="profileAlias"/>, or it isn't a Decision profile.
    /// </exception>
    /// <exception cref="AIProviderException">
    /// The provider returned an answer whose runtime type doesn't match <typeparamref name="TAnswer"/>
    /// — a provider bug, not a caller error (see the remarks on this interface).
    /// </exception>
    Task<AIDecisionResponse<TAnswer>> AskAsync<TAnswer>(
        string profileAlias,
        AIDecisionQuestion<TAnswer> question,
        string? state = null,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default)
        where TAnswer : AIDecisionAnswer;

    /// <summary>
    /// Asks a single question using an inline decision builder, with full observability
    /// (notifications, telemetry, duration tracking).
    /// </summary>
    /// <remarks>
    /// This is the primary entry point — the no-profile, <c>Guid</c>, and <c>string</c> overloads all
    /// delegate to this overload internally, the same way Chat's/SpeechToText's profile-id/alias
    /// overloads delegate to their own builder-based main path.
    /// </remarks>
    /// <typeparam name="TAnswer">The answer type the question resolves to.</typeparam>
    /// <param name="configure">Action to configure the inline decision via the builder.</param>
    /// <param name="question">The question to ask.</param>
    /// <param name="state">The content being judged, when there is any.</param>
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
    /// The provider returned an answer whose runtime type doesn't match <typeparamref name="TAnswer"/>
    /// — a provider bug, not a caller error (see the remarks on this interface).
    /// </exception>
    Task<AIDecisionResponse<TAnswer>> AskAsync<TAnswer>(
        Action<AIDecisionBuilder> configure,
        AIDecisionQuestion<TAnswer> question,
        string? state = null,
        CancellationToken cancellationToken = default)
        where TAnswer : AIDecisionAnswer;

    /// <summary>
    /// Asks every question in <paramref name="request"/> against the default Decision profile, in a
    /// single model call.
    /// </summary>
    /// <param name="request">The shared state and questions to ask.</param>
    /// <param name="options">Optional per-call overrides (e.g. model).</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The decision response, with one answer per question keyed by its id.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="request"/> is shaped incorrectly (e.g. no questions, duplicate or blank question
    /// ids, or any question shaped incorrectly for its concrete type). Rejected before any provider is
    /// reached — see <see cref="ValidatingDecisionClient"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// No default Decision profile is configured (neither stored nor via
    /// <c>AIOptions.DefaultDecisionProfileAlias</c>).
    /// </exception>
    /// <exception cref="AIProviderException">
    /// The provider's answers don't match what was asked (see ARCHITECTURE.md's "Checks") — a provider
    /// bug, not a caller error.
    /// </exception>
    Task<AIDecisionResponse> GetDecisionResponseAsync(
        AIDecisionRequest request,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the given profile by ID and asks it every question in <paramref name="request"/>, in a
    /// single model call.
    /// </summary>
    /// <param name="profileId">The ID of the Decision profile to use.</param>
    /// <param name="request">The shared state and questions to ask.</param>
    /// <param name="options">Optional per-call overrides (e.g. model).</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The decision response, with one answer per question keyed by its id.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="request"/> is shaped incorrectly. Rejected before any provider is reached — see
    /// <see cref="ValidatingDecisionClient"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// No profile matches <paramref name="profileId"/>, or it isn't a Decision profile.
    /// </exception>
    /// <exception cref="AIProviderException">
    /// The provider's answers don't match what was asked — a provider bug, not a caller error.
    /// </exception>
    Task<AIDecisionResponse> GetDecisionResponseAsync(
        Guid profileId,
        AIDecisionRequest request,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the given profile by alias and asks it every question in <paramref name="request"/>, in
    /// a single model call.
    /// </summary>
    /// <param name="profileAlias">The alias of the Decision profile to use.</param>
    /// <param name="request">The shared state and questions to ask.</param>
    /// <param name="options">Optional per-call overrides (e.g. model).</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The decision response, with one answer per question keyed by its id.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="request"/> is shaped incorrectly. Rejected before any provider is reached — see
    /// <see cref="ValidatingDecisionClient"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// No profile matches <paramref name="profileAlias"/>, or it isn't a Decision profile.
    /// </exception>
    /// <exception cref="AIProviderException">
    /// The provider's answers don't match what was asked — a provider bug, not a caller error.
    /// </exception>
    Task<AIDecisionResponse> GetDecisionResponseAsync(
        string profileAlias,
        AIDecisionRequest request,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asks every question in <paramref name="request"/> using an inline decision builder, with full
    /// observability (notifications, telemetry, duration tracking).
    /// </summary>
    /// <remarks>
    /// This is the primary entry point — the no-profile, <c>Guid</c>, and <c>string</c> overloads all
    /// delegate to this overload internally.
    /// </remarks>
    /// <param name="configure">Action to configure the inline decision via the builder.</param>
    /// <param name="request">The shared state and questions to ask.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The decision response, with one answer per question keyed by its id.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="request"/> is shaped incorrectly. Rejected before any provider is reached — see
    /// <see cref="ValidatingDecisionClient"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// No profile matches the builder's configured profile ID/alias (or none was configured and no
    /// default Decision profile exists), the resolved profile isn't a Decision profile, or a subscriber
    /// cancelled the execution via <see cref="AIDecisionExecutingNotification"/>.
    /// </exception>
    /// <exception cref="AIProviderException">
    /// The provider's answers don't match what was asked — a provider bug, not a caller error.
    /// </exception>
    Task<AIDecisionResponse> GetDecisionResponseAsync(
        Action<AIDecisionBuilder> configure,
        AIDecisionRequest request,
        CancellationToken cancellationToken = default);
}
