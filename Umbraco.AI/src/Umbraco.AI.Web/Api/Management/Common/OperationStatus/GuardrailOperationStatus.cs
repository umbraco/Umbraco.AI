namespace Umbraco.AI.Web.Api.Management.Common.OperationStatus;

/// <summary>
/// Defines operation status codes for guardrail operations.
/// </summary>
public enum GuardrailOperationStatus
{
    /// <summary>
    /// The guardrail was not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// A guardrail with the same alias already exists.
    /// </summary>
    DuplicateAlias
}
