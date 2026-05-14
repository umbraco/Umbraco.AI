using System.Text.Json.Nodes;

namespace Umbraco.AI.Core.PropertyValueOperations;

/// <summary>
/// Structured error payload returned by the property value dispatcher when an operation fails.
/// </summary>
/// <remarks>
/// The shape is intentionally LLM-friendly: a stable <see cref="Code"/> identifying the failure
/// class, a short human-readable <see cref="Message"/>, and an optional structured
/// <see cref="Details"/> payload for fields like <c>missingFields</c>, <c>unknownFields</c>, or
/// <c>allowedElementTypes</c> so the LLM can self-correct without further round-trips.
/// </remarks>
/// <param name="Code">A stable error code (e.g. <c>schema-mismatch</c>, <c>no-handler</c>, <c>block-not-found</c>).</param>
/// <param name="Message">A short human-readable description of the failure.</param>
/// <param name="Details">Optional structured details for self-correction.</param>
public sealed record AIPropertyValueOperationError(
    string Code,
    string Message,
    JsonObject? Details = null)
{
    /// <summary>Common error codes recognised by the dispatcher and handlers.</summary>
    public static class Codes
    {
        /// <summary>The supplied path is empty, malformed, or violates the alternation rule.</summary>
        public const string InvalidPath = "invalid-path";

        /// <summary>No handler is registered for the requested editor schema alias.</summary>
        public const string NoHandler = "no-handler";

        /// <summary>The supplied root value is missing or has the wrong shape.</summary>
        public const string InvalidRootValue = "invalid-root-value";

        /// <summary>A property along the path could not be resolved against the schema.</summary>
        public const string PropertyNotFound = "property-not-found";

        /// <summary>A block referenced in the path was not present in its parent collection.</summary>
        public const string BlockNotFound = "block-not-found";

        /// <summary>Supplied values did not match the editor's value schema.</summary>
        public const string SchemaMismatch = "schema-mismatch";

        /// <summary>The element type alias or key supplied to AddItem is not allowed by the parent property.</summary>
        public const string ElementTypeNotAllowed = "element-type-not-allowed";

        /// <summary>The handler does not support this operation for the targeted editor (e.g. RTE AddItem).</summary>
        public const string OperationNotSupported = "operation-not-supported";

        /// <summary>The supplied position is outside the collection's bounds.</summary>
        public const string PositionOutOfRange = "position-out-of-range";

        /// <summary>An unexpected internal error occurred.</summary>
        public const string Internal = "internal-error";
    }
}
