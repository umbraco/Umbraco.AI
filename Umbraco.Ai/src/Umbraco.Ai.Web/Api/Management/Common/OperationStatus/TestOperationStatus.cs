namespace Umbraco.Ai.Web.Api.Management.Common.OperationStatus;

/// <summary>
/// Operation status codes for test operations.
/// </summary>
public enum TestOperationStatus
{
    /// <summary>
    /// The operation completed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// The test was not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// The alias is already in use by another test.
    /// </summary>
    DuplicateAlias,

    /// <summary>
    /// The test feature was not found.
    /// </summary>
    TestFeatureNotFound,

    /// <summary>
    /// An unknown error occurred.
    /// </summary>
    Unknown
}
