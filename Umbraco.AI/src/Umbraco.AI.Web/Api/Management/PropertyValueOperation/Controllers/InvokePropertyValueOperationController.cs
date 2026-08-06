using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.PropertyValueOperations;
using Umbraco.AI.Web.Api.Management.PropertyValueOperation.Models;

namespace Umbraco.AI.Web.Api.Management.PropertyValueOperation.Controllers;

/// <summary>
/// Stateless transformation endpoint: accepts a property value, applies the requested operation
/// via <see cref="IAIPropertyValueDispatcher"/>, and returns the mutated value.
/// </summary>
/// <remarks>
/// <para>
/// The endpoint never reads from or writes to the database. It is consumed by frontend tools
/// (which transport the workspace's staged value over HTTP) and is conceptually identical to how
/// future server-side AI tools will call the dispatcher in-process.
/// </para>
/// <para>
/// The auth boundary is deliberately just <c>BackOfficeAccess</c>, inherited from the base
/// controller — this endpoint does NOT carry the <c>SectionAccessAI</c> policy that the rest of the
/// AI management API uses. Because the operation is a pure transform over a value the caller
/// already supplied, granting it exposes no data the caller could not already read and no write
/// the caller could not already perform: persistence happens later through the CMS content APIs,
/// which enforce their own permissions on the document being saved.
/// </para>
/// <para>
/// Do NOT add <c>SectionAccessAI</c> here. Gating this on the AI section would mean any editor
/// using an AI feature in the content workspace (e.g. Copilot's <c>set_value</c> tool) would also
/// need full access to Connections, Profiles, Agents and Guardrails administration. See issue #306.
/// </para>
/// </remarks>
[ApiVersion("1.0")]
public sealed class InvokePropertyValueOperationController : PropertyValueOperationControllerBase
{
    private readonly IAIPropertyValueDispatcher _dispatcher;

    /// <summary>
    /// Initializes a new <see cref="InvokePropertyValueOperationController"/>.
    /// </summary>
    public InvokePropertyValueOperationController(IAIPropertyValueDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    /// <summary>
    /// Applies a single property value operation.
    /// </summary>
    /// <param name="request">The dispatch request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The dispatch result.</returns>
    [HttpPost]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(PropertyValueOperationResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PropertyValueOperationResponseModel>> Invoke(
        [FromBody] PropertyValueOperationRequestModel request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var dispatchRequest = new AIPropertyValueDispatchRequest(
            Path: request.Path?.ToArray() ?? Array.Empty<AIPropertyPathSegment>(),
            Operation: request.Operation,
            Args: request.Args,
            RootValue: request.RootValue,
            DocumentMetadata: request.DocumentMetadata);

        var result = await _dispatcher.DispatchAsync(dispatchRequest, cancellationToken).ConfigureAwait(false);

        return Ok(new PropertyValueOperationResponseModel
        {
            Success = result.Success,
            NewRootValue = result.NewRootValue,
            BlockKey = result.BlockKey,
            Error = result.Error,
        });
    }
}
