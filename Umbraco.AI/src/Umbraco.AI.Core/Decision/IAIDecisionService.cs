using System.Diagnostics.CodeAnalysis;

namespace Umbraco.AI.Core.Decision;

/// <summary>
/// Defines an AI decision service that resolves a Decision profile (by ID or alias) and answers a
/// typed <see cref="AIDecisionQuestion"/> through it. This service acts as a thin layer over
/// <see cref="IAIDecisionClientFactory"/>, adding Umbraco-specific profile/connection resolution —
/// mirrors <see cref="Umbraco.AI.Core.SpeechToText.IAISpeechToTextService"/>'s profile-resolving shape.
/// </summary>
/// <remarks>
/// Unlike an HTTP request DTO, none of these methods take a single "ID or alias" parameter type —
/// Core services never do (see <see cref="Umbraco.AI.Core.Chat.IAIChatService"/>/
/// <see cref="Umbraco.AI.Core.SpeechToText.IAISpeechToTextService"/>, whose non-obsolete surface is the
/// builder pattern with distinct <c>WithProfile(Guid)</c>/<c>WithProfile(string)</c> methods). The two
/// profile-typed overloads below are thin convenience wrappers over the builder-based
/// <see cref="AskAsync(Action{AIDecisionBuilder}, AIDecisionQuestion, CancellationToken)"/> overload —
/// Decision has no shipped legacy surface to preserve, so neither needs an <see cref="ObsoleteAttribute"/>
/// the way Chat's/SpeechToText's equivalents do.
/// </remarks>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public interface IAIDecisionService
{
    /// <summary>
    /// Resolves the given profile by ID and asks it the supplied <paramref name="question"/>.
    /// </summary>
    /// <param name="profileId">The ID of the Decision profile to use.</param>
    /// <param name="question">The question to ask.</param>
    /// <param name="options">Optional per-call overrides (e.g. model).</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The typed decision response.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="question"/> is shaped incorrectly for its <see cref="AIDecisionQuestion.Kind"/>
    /// (e.g. an empty prompt, or a <see cref="AIDecisionKind.Choice"/> question with fewer than two
    /// choices). Rejected before any provider is reached — see <see cref="ValidatingDecisionClient"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// No profile matches <paramref name="profileId"/>, or it isn't a Decision profile.
    /// </exception>
    Task<AIDecisionResponse> AskAsync(
        Guid profileId,
        AIDecisionQuestion question,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the given profile by alias and asks it the supplied <paramref name="question"/>.
    /// </summary>
    /// <param name="profileAlias">The alias of the Decision profile to use.</param>
    /// <param name="question">The question to ask.</param>
    /// <param name="options">Optional per-call overrides (e.g. model).</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The typed decision response.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="question"/> is shaped incorrectly for its <see cref="AIDecisionQuestion.Kind"/>
    /// (e.g. an empty prompt, or a <see cref="AIDecisionKind.Choice"/> question with fewer than two
    /// choices). Rejected before any provider is reached — see <see cref="ValidatingDecisionClient"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// No profile matches <paramref name="profileAlias"/>, or it isn't a Decision profile.
    /// </exception>
    Task<AIDecisionResponse> AskAsync(
        string profileAlias,
        AIDecisionQuestion question,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asks a decision using an inline decision builder, with full observability
    /// (notifications, telemetry, duration tracking).
    /// </summary>
    /// <remarks>
    /// This is the primary entry point — <see cref="AskAsync(Guid, AIDecisionQuestion, AIDecisionOptions?, CancellationToken)"/>
    /// and <see cref="AskAsync(string, AIDecisionQuestion, AIDecisionOptions?, CancellationToken)"/> both
    /// delegate to this overload internally, the same way Chat's/SpeechToText's profile-id/alias
    /// overloads delegate to their own builder-based main path.
    /// </remarks>
    /// <param name="configure">Action to configure the inline decision via the builder.</param>
    /// <param name="question">The question to ask.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The typed decision response.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="question"/> is shaped incorrectly for its <see cref="AIDecisionQuestion.Kind"/>.
    /// Rejected before any provider is reached — see <see cref="ValidatingDecisionClient"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// No profile matches the builder's configured profile ID/alias (or none was configured and no
    /// default Decision profile exists), the resolved profile isn't a Decision profile, or a subscriber
    /// cancelled the execution via <see cref="AIDecisionExecutingNotification"/>.
    /// </exception>
    Task<AIDecisionResponse> AskAsync(
        Action<AIDecisionBuilder> configure,
        AIDecisionQuestion question,
        CancellationToken cancellationToken = default);
}
