namespace Umbraco.Ai.Web.Api.Management.Common.OperationStatus;

/// <summary>
/// Operation status codes for profile operations.
/// </summary>
public enum ProfileOperationStatus
{
    /// <summary>
    /// The operation completed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// The profile was not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// The alias is already in use by another profile.
    /// </summary>
    DuplicateAlias,

    /// <summary>
    /// The connection specified does not exist.
    /// </summary>
    ConnectionNotFound,

    /// <summary>
    /// The provider specified does not exist.
    /// </summary>
    ProviderNotFound,

    /// <summary>
    /// The model specified is not valid for the provider.
    /// </summary>
    InvalidModel,

    /// <summary>
    /// The capability does not match the profile type.
    /// </summary>
    InvalidCapability,

    /// <summary>
    /// An unknown error occurred.
    /// </summary>
    Unknown
}
