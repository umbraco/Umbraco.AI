using Umbraco.Cms.Core;

namespace Umbraco.AI.Web.Api.Management.Common.OperationStatus;

/// <summary>
/// Defines operation status codes for context operations.
/// </summary>
public enum ContextOperationStatus
{
    /// <summary>
    /// The context was not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// A context with the same alias already exists.
    /// </summary>
    DuplicateAlias
}
