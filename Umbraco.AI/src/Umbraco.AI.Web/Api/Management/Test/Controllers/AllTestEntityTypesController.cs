using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.EntityAdapter;
using Umbraco.AI.Web.Api.Management.Test.Models;
using Umbraco.AI.Web.Authorization;

namespace Umbraco.AI.Web.Api.Management.Test.Controllers;

/// <summary>
/// Controller to get registered entity types and their sub-types for test mock entity editing.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class AllTestEntityTypesController : TestControllerBase
{
    private readonly AIEntityAdapterCollection _adapters;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllTestEntityTypesController"/> class.
    /// </summary>
    public AllTestEntityTypesController(AIEntityAdapterCollection adapters)
    {
        _adapters = adapters;
    }

    /// <summary>
    /// Get all registered entity types with metadata.
    /// </summary>
    /// <returns>A list of registered entity types.</returns>
    [HttpGet("entity-types")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<TestEntityTypeResponseModel>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TestEntityTypeResponseModel>> GetAllEntityTypes()
    {
        var entityTypes = _adapters.GetEntityTypeAdapters()
            .Select(a => new TestEntityTypeResponseModel
            {
                EntityType = a.EntityType!,
                Name = a.Name,
                Icon = a.Icon,
                HasSubTypes = a.HasSubTypes
            })
            .OrderBy(e => e.Name);

        return Ok(entityTypes);
    }

    /// <summary>
    /// Get sub-types for a specific entity type (e.g., content types for documents).
    /// </summary>
    /// <param name="entityType">The entity type to get sub-types for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of sub-types for the entity type.</returns>
    [HttpGet("entity-types/{entityType}/sub-types")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<TestEntitySubTypeResponseModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<TestEntitySubTypeResponseModel>>> GetEntitySubTypes(
        string entityType,
        CancellationToken cancellationToken = default)
    {
        var adapter = _adapters.GetEntityTypeAdapters()
            .FirstOrDefault(a => string.Equals(a.EntityType, entityType, StringComparison.OrdinalIgnoreCase));

        if (adapter == null)
        {
            return NotFound(CreateProblemDetails(
                "Entity type not found",
                $"No adapter registered for entity type '{entityType}'."));
        }

        if (!adapter.HasSubTypes)
        {
            return Ok(Enumerable.Empty<TestEntitySubTypeResponseModel>());
        }

        var subTypes = await adapter.GetEntitySubTypesAsync(cancellationToken);

        var response = subTypes.Select(st => new TestEntitySubTypeResponseModel
        {
            Alias = st.Alias,
            Name = st.Name,
            Icon = st.Icon,
            Description = st.Description,
            Unique = st.Unique
        });

        return Ok(response);
    }
}
