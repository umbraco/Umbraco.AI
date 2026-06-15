namespace Umbraco.AI.Core.Providers.Errors;

/// <summary>
/// A provider SDK failure that has been classified into a user-safe <see cref="AIProviderErrorInfo"/>.
/// </summary>
/// <remarks>
/// <para>
/// Thrown by the error-classifying client decorators applied in the capability factories. Because
/// classification happens where the originating <see cref="IAIProvider"/> is known, the provider's
/// own <see cref="IAIProvider.ClassifyError"/> produces the <see cref="Info"/> — no exception-type
/// sniffing required.
/// </para>
/// <para>
/// <see cref="Exception.Message"/> is the user-safe message, so call sites that surface the raw
/// message still show something presentable. The original exception is preserved as
/// <see cref="Exception.InnerException"/> for logs and diagnostics.
/// </para>
/// </remarks>
public sealed class AIProviderException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIProviderException"/> class.
    /// </summary>
    /// <param name="info">The classified error information.</param>
    /// <param name="innerException">The original provider SDK exception.</param>
    public AIProviderException(AIProviderErrorInfo info, Exception? innerException = null)
        : base(info.UserMessage, innerException)
    {
        Info = info;
    }

    /// <summary>
    /// The classified error information.
    /// </summary>
    public AIProviderErrorInfo Info { get; }

    /// <summary>
    /// The normalised error category.
    /// </summary>
    public AIProviderErrorCategory Category => Info.Category;

    /// <summary>
    /// A message safe to render in user-facing surfaces.
    /// </summary>
    public string UserMessage => Info.UserMessage;

    /// <summary>
    /// The original provider-specific error code, when available. Surface in telemetry, not the UI.
    /// </summary>
    public string? ProviderCode => Info.ProviderCode;
}
