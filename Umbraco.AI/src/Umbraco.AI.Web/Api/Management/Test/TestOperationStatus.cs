using Umbraco.AI.Web.Api.Management.Common.OperationStatus;

namespace Umbraco.AI.Web.Api.Management.Test;

/// <summary>
/// Represents the status of a test operation.
/// </summary>
public enum TestOperationStatus
{
    /// <summary>
    /// The operation succeeded.
    /// </summary>
    [OperationStatusCode(StatusCodes.Status200OK)]
    Success,

    /// <summary>
    /// The test was not found.
    /// </summary>
    [OperationStatusCode(StatusCodes.Status404NotFound)]
    NotFound,

    /// <summary>
    /// A test with the same alias already exists.
    /// </summary>
    [OperationStatusCode(StatusCodes.Status400BadRequest)]
    DuplicateAlias,

    /// <summary>
    /// The specified test type is invalid.
    /// </summary>
    [OperationStatusCode(StatusCodes.Status400BadRequest)]
    InvalidTestType,

    /// <summary>
    /// The specified target is invalid.
    /// </summary>
    [OperationStatusCode(StatusCodes.Status400BadRequest)]
    InvalidTarget,
}
