using System.Diagnostics.CodeAnalysis;
using Umbraco.AI.Core.Providers.Errors;

namespace Umbraco.AI.Core.Extraction;

/// <summary>
/// Defines an AI extraction service that resolves an Extraction profile (by ID or alias) and reads one
/// or more typed <see cref="AIExtractionField"/>s out of text through it. This service acts as a thin
/// layer over <see cref="IAIExtractionClientFactory"/>, adding Umbraco-specific profile/connection
/// resolution — mirrors <see cref="Umbraco.AI.Core.SpeechToText.IAISpeechToTextService"/>'s
/// profile-resolving shape.
/// </summary>
/// <remarks>
/// <para>
/// Core services never take a single "ID or alias" parameter type (see
/// <see cref="Umbraco.AI.Core.Chat.IAIChatService"/>/<see cref="Umbraco.AI.Core.SpeechToText.IAISpeechToTextService"/>,
/// whose surface is the builder pattern with distinct <c>WithProfile(Guid)</c>/<c>WithProfile(string)</c>
/// methods). The profile-typed and no-profile overloads below are thin convenience wrappers over the
/// builder-based main overload of each method. Extraction has no shipped legacy surface to preserve, so
/// none of them needs an <see cref="ObsoleteAttribute"/>.
/// </para>
/// <para>
/// Every method throws <see cref="InvalidOperationException"/> while the
/// <c>Umbraco:AI:Experimental:Extraction</c> flag is off.
/// </para>
/// </remarks>
[Experimental(AIExtractionDiagnostics.DiagnosticId)]
public interface IAIExtractionService
{
    /// <summary>
    /// Extracts a single field against the default Extraction profile.
    /// </summary>
    /// <remarks>
    /// A thin helper over <see cref="GetExtractionResponseAsync(AIExtractionRequest, AIExtractionOptions?, CancellationToken)"/>:
    /// it builds a one-field <see cref="AIExtractionRequest"/> (assigning an internal id when
    /// <paramref name="field"/>'s <see cref="AIExtractionField.Id"/> is null), runs it through the same
    /// pipeline, and unwraps the single answer. The primary check that a provider answered the wrong
    /// shape happens inside <see cref="AIErrorClassifyingExtractionClient"/>, which sits inside the
    /// tracking middleware so the mismatch is recorded as a failure — this method only keeps a narrowing
    /// guard afterwards as defence in depth.
    /// </remarks>
    /// <typeparam name="TAnswer">The answer type the field resolves to.</typeparam>
    /// <param name="field">The field to extract.</param>
    /// <param name="state">The text to extract from.</param>
    /// <param name="options">Optional per-call overrides (e.g. model).</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The typed extraction response.</returns>
    /// <exception cref="ArgumentException">
    /// The field or state is invalid (e.g. blank instructions or state). Rejected before any provider is
    /// reached — see <see cref="ValidatingExtractionClient"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The experimental flag is off, or no default Extraction profile is configured (neither stored nor
    /// via <c>AIOptions.DefaultExtractionProfileAlias</c>).
    /// </exception>
    /// <exception cref="AIProviderException">
    /// The provider's answer doesn't match what was asked — a provider bug, not a caller error.
    /// </exception>
    Task<AIExtractionResponse<TAnswer>> ExtractAsync<TAnswer>(
        AIExtractionField<TAnswer> field,
        string? state = null,
        AIExtractionOptions? options = null,
        CancellationToken cancellationToken = default)
        where TAnswer : AIExtractionAnswer;

    /// <summary>
    /// Resolves the given profile by ID and extracts a single <paramref name="field"/>.
    /// </summary>
    /// <typeparam name="TAnswer">The answer type the field resolves to.</typeparam>
    /// <param name="profileId">The ID of the Extraction profile to use.</param>
    /// <param name="field">The field to extract.</param>
    /// <param name="state">The text to extract from.</param>
    /// <param name="options">Optional per-call overrides (e.g. model).</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The typed extraction response.</returns>
    /// <exception cref="ArgumentException">The field or state is invalid.</exception>
    /// <exception cref="InvalidOperationException">
    /// The experimental flag is off, or no profile matches <paramref name="profileId"/>, or it isn't an
    /// Extraction profile.
    /// </exception>
    /// <exception cref="AIProviderException">The provider's answer doesn't match what was asked.</exception>
    Task<AIExtractionResponse<TAnswer>> ExtractAsync<TAnswer>(
        Guid profileId,
        AIExtractionField<TAnswer> field,
        string? state = null,
        AIExtractionOptions? options = null,
        CancellationToken cancellationToken = default)
        where TAnswer : AIExtractionAnswer;

    /// <summary>
    /// Resolves the given profile by alias and extracts a single <paramref name="field"/>.
    /// </summary>
    /// <typeparam name="TAnswer">The answer type the field resolves to.</typeparam>
    /// <param name="profileAlias">The alias of the Extraction profile to use.</param>
    /// <param name="field">The field to extract.</param>
    /// <param name="state">The text to extract from.</param>
    /// <param name="options">Optional per-call overrides (e.g. model).</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The typed extraction response.</returns>
    /// <exception cref="ArgumentException">The field or state is invalid.</exception>
    /// <exception cref="InvalidOperationException">
    /// The experimental flag is off, or no profile matches <paramref name="profileAlias"/>, or it isn't an
    /// Extraction profile.
    /// </exception>
    /// <exception cref="AIProviderException">The provider's answer doesn't match what was asked.</exception>
    Task<AIExtractionResponse<TAnswer>> ExtractAsync<TAnswer>(
        string profileAlias,
        AIExtractionField<TAnswer> field,
        string? state = null,
        AIExtractionOptions? options = null,
        CancellationToken cancellationToken = default)
        where TAnswer : AIExtractionAnswer;

    /// <summary>
    /// Extracts a single field using an inline extraction builder, with full observability
    /// (notifications, telemetry, duration tracking).
    /// </summary>
    /// <remarks>
    /// This is the primary single-field entry point — the no-profile, <c>Guid</c>, and <c>string</c>
    /// overloads all delegate to this overload internally. The builder must set an alias with <c>WithAlias(...)</c>.
    /// </remarks>
    /// <typeparam name="TAnswer">The answer type the field resolves to.</typeparam>
    /// <param name="configure">Action to configure the inline extraction via the builder.</param>
    /// <param name="field">The field to extract.</param>
    /// <param name="state">The text to extract from.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The typed extraction response.</returns>
    /// <exception cref="ArgumentException">The field or state is invalid.</exception>
    /// <exception cref="InvalidOperationException">
    /// The experimental flag is off, no profile matches the builder's configured profile ID/alias (or none
    /// was configured and no default Extraction profile exists), the resolved profile isn't an Extraction
    /// profile, or a subscriber cancelled the execution via <see cref="AIExtractionExecutingNotification"/>.
    /// </exception>
    /// <exception cref="AIProviderException">The provider's answer doesn't match what was asked.</exception>
    Task<AIExtractionResponse<TAnswer>> ExtractAsync<TAnswer>(
        Action<AIExtractionBuilder> configure,
        AIExtractionField<TAnswer> field,
        string? state = null,
        CancellationToken cancellationToken = default)
        where TAnswer : AIExtractionAnswer;

    /// <summary>
    /// Extracts every field in <paramref name="request"/> against the default Extraction profile, in a
    /// single model call.
    /// </summary>
    /// <param name="request">The shared text and fields to extract.</param>
    /// <param name="options">Optional per-call overrides (e.g. model).</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The extraction response, with one answer per field keyed by its id.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="request"/> is shaped incorrectly (e.g. no fields, duplicate or malformed field
    /// ids, blank instructions or state). Rejected before any provider is reached — see
    /// <see cref="ValidatingExtractionClient"/>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The experimental flag is off, or no default Extraction profile is configured (neither stored nor
    /// via <c>AIOptions.DefaultExtractionProfileAlias</c>).
    /// </exception>
    /// <exception cref="AIProviderException">
    /// The provider's answers don't match what was asked — a provider bug, not a caller error.
    /// </exception>
    Task<AIExtractionResponse> GetExtractionResponseAsync(
        AIExtractionRequest request,
        AIExtractionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the given profile by ID and extracts every field in <paramref name="request"/>, in a
    /// single model call.
    /// </summary>
    /// <param name="profileId">The ID of the Extraction profile to use.</param>
    /// <param name="request">The shared text and fields to extract.</param>
    /// <param name="options">Optional per-call overrides (e.g. model).</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The extraction response, with one answer per field keyed by its id.</returns>
    /// <exception cref="ArgumentException"><paramref name="request"/> is shaped incorrectly.</exception>
    /// <exception cref="InvalidOperationException">
    /// The experimental flag is off, or no profile matches <paramref name="profileId"/>, or it isn't an
    /// Extraction profile.
    /// </exception>
    /// <exception cref="AIProviderException">The provider's answers don't match what was asked.</exception>
    Task<AIExtractionResponse> GetExtractionResponseAsync(
        Guid profileId,
        AIExtractionRequest request,
        AIExtractionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the given profile by alias and extracts every field in <paramref name="request"/>, in a
    /// single model call.
    /// </summary>
    /// <param name="profileAlias">The alias of the Extraction profile to use.</param>
    /// <param name="request">The shared text and fields to extract.</param>
    /// <param name="options">Optional per-call overrides (e.g. model).</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The extraction response, with one answer per field keyed by its id.</returns>
    /// <exception cref="ArgumentException"><paramref name="request"/> is shaped incorrectly.</exception>
    /// <exception cref="InvalidOperationException">
    /// The experimental flag is off, or no profile matches <paramref name="profileAlias"/>, or it isn't an
    /// Extraction profile.
    /// </exception>
    /// <exception cref="AIProviderException">The provider's answers don't match what was asked.</exception>
    Task<AIExtractionResponse> GetExtractionResponseAsync(
        string profileAlias,
        AIExtractionRequest request,
        AIExtractionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts every field in <paramref name="request"/> using an inline extraction builder, with full
    /// observability (notifications, telemetry, duration tracking).
    /// </summary>
    /// <remarks>
    /// This is the primary entry point — the no-profile, <c>Guid</c>, and <c>string</c> overloads all
    /// delegate to this overload internally. The builder must set an alias with <c>WithAlias(...)</c>.
    /// </remarks>
    /// <param name="configure">Action to configure the inline extraction via the builder.</param>
    /// <param name="request">The shared text and fields to extract.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>The extraction response, with one answer per field keyed by its id.</returns>
    /// <exception cref="ArgumentException"><paramref name="request"/> is shaped incorrectly.</exception>
    /// <exception cref="InvalidOperationException">
    /// The experimental flag is off, no profile matches the builder's configured profile ID/alias (or none
    /// was configured and no default Extraction profile exists), the resolved profile isn't an Extraction
    /// profile, or a subscriber cancelled the execution via <see cref="AIExtractionExecutingNotification"/>.
    /// </exception>
    /// <exception cref="AIProviderException">The provider's answers don't match what was asked.</exception>
    Task<AIExtractionResponse> GetExtractionResponseAsync(
        Action<AIExtractionBuilder> configure,
        AIExtractionRequest request,
        CancellationToken cancellationToken = default);
}
