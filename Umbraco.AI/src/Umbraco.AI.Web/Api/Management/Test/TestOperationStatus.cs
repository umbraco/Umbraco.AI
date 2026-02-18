namespace Umbraco.AI.Web.Api.Management.Test;

/// <summary>
/// Represents the status of a test operation.
/// </summary>
public enum TestOperationStatus
{
    /// <summary>
    /// The operation succeeded.
    /// </summary>
    Success,

    /// <summary>
    /// The test was not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// A test with the same alias already exists.
    /// </summary>
    DuplicateAlias,

    /// <summary>
    /// The specified test type is invalid.
    /// </summary>
    InvalidTestType,

    /// <summary>
    /// The specified target is invalid.
    /// </summary>
    InvalidTarget,
}
