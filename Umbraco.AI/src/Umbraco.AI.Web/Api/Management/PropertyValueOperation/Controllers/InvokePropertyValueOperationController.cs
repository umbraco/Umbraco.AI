using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.PropertyValueOperations;
using Umbraco.AI.Web.Api.Management.PropertyValueOperation.Models;
using Umbraco.AI.Web.Authorization;

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
/// The auth boundary is <c>SectionAccessAI</c>; the same surface as other AI management APIs.
/// Frontend tools delegate authorization to this policy and do not duplicate it client-side.
/// </para>
/// </remarks>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
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
            RootEditorSchemaAlias: request.RootEditorSchemaAlias,
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
