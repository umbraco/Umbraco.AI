namespace Umbraco.Ai.Web.Api.Management.Common.OperationStatus;

/// <summary>
/// Operation status codes for connection operations.
/// </summary>
public enum ConnectionOperationStatus
{
    /// <summary>
    /// The operation completed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// The connection was not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// The provider specified does not exist.
    /// </summary>
    ProviderNotFound,

    /// <summary>
    /// The connection settings are invalid.
    /// </summary>
    InvalidSettings,

    /// <summary>
    /// The connection is in use by one or more profiles.
    /// </summary>
    InUse,

    /// <summary>
    /// The connection test failed.
    /// </summary>
    TestFailed,

    /// <summary>
    /// An unknown error occurred.
    /// </summary>
    Unknown
}
