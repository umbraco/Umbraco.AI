using System.Text.Json.Nodes;
using Umbraco.AI.Core.PropertyValueOperations;

namespace Umbraco.AI.Web.Api.Management.PropertyValueOperation.Models;

/// <summary>
/// Response payload returned by the property value operation endpoint.
/// </summary>
/// <remarks>
/// On success, <see cref="NewRootValue"/> is set (and <see cref="BlockKey"/> is populated for
/// AddItem operations). On failure, <see cref="Error"/> is set with a structured payload the
/// caller can surface to the LLM for self-correction.
/// </remarks>
public sealed class PropertyValueOperationResponseModel
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The mutated root property value. <c>null</c> when the operation cleared the value or when
    /// the operation failed.
    /// </summary>
    public JsonNode? NewRootValue { get; set; }

    /// <summary>
    /// The freshly-minted block key, populated for AddItem operations.
    /// </summary>
    public Guid? BlockKey { get; set; }

    /// <summary>
    /// Structured error payload when <see cref="Success"/> is <c>false</c>; <c>null</c> on success.
    /// </summary>
    public AIPropertyValueOperationError? Error { get; set; }
}
