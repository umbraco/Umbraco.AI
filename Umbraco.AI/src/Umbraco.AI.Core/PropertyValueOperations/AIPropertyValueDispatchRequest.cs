using System.Text.Json.Nodes;

namespace Umbraco.AI.Core.PropertyValueOperations;

/// <summary>
/// Inputs for a single property value dispatch.
/// </summary>
/// <remarks>
/// The dispatcher is stateless: <see cref="RootValue"/> is the single source of truth and is
/// supplied by the caller (frontend tools send the workspace's staged value; future server-side
/// tools send the database-read value). The dispatcher never reads or writes data.
/// </remarks>
/// <param name="Path">
/// Path to the leaf the operation acts on. Property aliases at even indices, block selectors at
/// odd indices. Must be non-empty.
/// </param>
/// <param name="Operation">The kind of operation to perform.</param>
/// <param name="Args">
/// Operation-specific argument payload. Shape varies per operation:
/// <list type="bullet">
///   <item><description><see cref="AIPropertyOperation.AddItem"/>: <see cref="AIAddItemArgs"/> serialised as JSON.</description></item>
///   <item><description><see cref="AIPropertyOperation.RemoveItem"/>: <c>{ "blockKey": "&lt;guid&gt;" }</c>.</description></item>
///   <item><description><see cref="AIPropertyOperation.MoveItem"/>: <c>{ "blockKey": "&lt;guid&gt;", "position": &lt;int&gt; }</c>.</description></item>
///   <item><description><see cref="AIPropertyOperation.SetValue"/>: <c>{ "value": &lt;any&gt; }</c>.</description></item>
///   <item><description><see cref="AIPropertyOperation.ClearValue"/>: <c>null</c>.</description></item>
/// </list>
/// </param>
/// <param name="RootValue">The current value of the root property the path begins in.</param>
/// <param name="RootEditorSchemaAlias">
/// Editor schema alias of the root property (e.g. <c>Umbraco.BlockList</c>). Determines which
/// handler the dispatcher uses for the first descent step.
/// </param>
/// <param name="DocumentMetadata">Document-level metadata supplied by the caller.</param>
public sealed record AIPropertyValueDispatchRequest(
    IReadOnlyList<AIPropertyPathSegment> Path,
    AIPropertyOperation Operation,
    JsonNode? Args,
    JsonNode? RootValue,
    string RootEditorSchemaAlias,
    AIDocumentMetadata DocumentMetadata);
