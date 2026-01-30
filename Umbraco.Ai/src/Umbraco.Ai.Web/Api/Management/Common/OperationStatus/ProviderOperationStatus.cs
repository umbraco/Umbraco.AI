namespace Umbraco.Ai.Web.Api.Management.Common.OperationStatus;

/// <summary>
/// Operation status codes for provider operations.
/// </summary>
public enum ProviderOperationStatus
{
    /// <summary>
    /// The operation completed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// The provider was not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// An unknown error occurred.
    /// </summary>
    Unknown
}
