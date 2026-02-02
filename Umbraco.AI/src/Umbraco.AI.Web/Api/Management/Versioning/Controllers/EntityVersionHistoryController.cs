using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Versioning;
using Umbraco.AI.Web.Api.Management.Common.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.AI.Web.Api.Management.Versioning.Controllers;

/// <summary>
/// Unified controller for entity version history operations.
/// </summary>
/// <remarks>
/// This controller provides a single endpoint for version history operations across all entity types
/// (Connection, Profile, Context, Prompt, Agent). The entity type is specified as a route parameter.
/// </remarks>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class EntityVersionHistoryController : VersioningControllerBase
{
    private readonly IAIEntityVersionService _versionService;
    private readonly AIVersionableEntityAdapterCollection _entityTypes;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityVersionHistoryController"/> class.
    /// </summary>
    public EntityVersionHistoryController(
        IAIEntityVersionService versionService,
        AIVersionableEntityAdapterCollection entityTypes,
        IUmbracoMapper umbracoMapper)
    {
        _versionService = versionService;
        _entityTypes = entityTypes;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Get version history for an entity.
    /// </summary>
    /// <param name="entityType">The entity type (e.g., "connection", "profile", "context").</param>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <param name="skip">Number of versions to skip (for pagination).</param>
    /// <param name="take">Number of versions to return (for pagination).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The version history for the entity.</returns>
    [HttpGet($"{{{nameof(entityType)}}}/{{{nameof(entityId)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(EntityVersionHistoryResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVersionHistory(
        [FromRoute] string entityType,
        [FromRoute] Guid entityId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 10,
        CancellationToken cancellationToken = default)
    {
        // Get the entity type handler (case-insensitive)
        var handler = _entityTypes.GetByTypeName(entityType);
        if (handler is null)
        {
            return UnknownEntityType(entityType);
        }

        // Get the current entity to include as the current version
        var currentEntity = await handler.GetEntityAsync(entityId, cancellationToken);
        if (currentEntity is not IAIAuditableEntity auditable)
        {
            // Entity not found or doesn't implement IAIAuditableEntity
            return NotFound(CreateProblemDetails(
                "Entity not found",
                $"The {entityType} with ID {entityId} was not found."));
        }

        // Create the current version entry from the live entity
        var currentVersionModel = new EntityVersionResponseModel
        {
            Id = auditable.Id,
            EntityId = entityId,
            Version = auditable is IAIVersionableEntity versionable ? versionable.Version : 1,
            DateCreated = auditable.DateModified,
            CreatedByUserId = auditable.ModifiedByUserId
        };

        // Calculate pagination parameters for historical versions
        // The current version occupies position 0, so we need to adjust skip/take for historical query
        var includeCurrentVersion = skip == 0;
        var historicalSkip = skip > 0 ? skip - 1 : 0;
        var historicalTake = includeCurrentVersion ? Math.Max(0, take - 1) : take;

        // Get historical versions with pagination from the service
        var (historicalVersions, historicalTotal) = await _versionService.GetVersionHistoryAsync(
            entityId,
            handler.EntityTypeName,
            historicalSkip,
            historicalTake,
            cancellationToken);

        var pagedVersions = new List<EntityVersionResponseModel>();

        // Add current version first if it's in the requested page
        if (includeCurrentVersion)
        {
            pagedVersions.Add(currentVersionModel);
        }

        // Add historical versions
        pagedVersions.AddRange(historicalVersions.Select(v => _umbracoMapper.Map<EntityVersionResponseModel>(v)!));

        // Total includes current version + all historical versions
        var totalVersions = historicalTotal + 1;

        return Ok(new EntityVersionHistoryResponseModel
        {
            CurrentVersion = currentVersionModel.Version,
            TotalVersions = totalVersions,
            Versions = pagedVersions
        });
    }

    /// <summary>
    /// Get a specific version snapshot for an entity.
    /// </summary>
    /// <param name="entityType">The entity type (e.g., "connection", "profile", "context").</param>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <param name="entityVersion">The version number to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The version record.</returns>
    [HttpGet($"{{{nameof(entityType)}}}/{{{nameof(entityId)}}}/{{{nameof(entityVersion)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(EntityVersionResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVersion(
        [FromRoute] string entityType,
        [FromRoute] Guid entityId,
        [FromRoute] int entityVersion,
        CancellationToken cancellationToken = default)
    {
        // Normalize entity type (case-insensitive)
        var normalizedType = NormalizeEntityType(entityType);
        if (normalizedType is null)
        {
            return UnknownEntityType(entityType);
        }

        var versionRecord = await _versionService.GetVersionAsync(entityId, normalizedType, entityVersion, cancellationToken);
        if (versionRecord is null)
        {
            return VersionNotFound(entityVersion);
        }

        return Ok(_umbracoMapper.Map<EntityVersionResponseModel>(versionRecord));
    }

    /// <summary>
    /// Compare two versions of an entity.
    /// </summary>
    /// <param name="entityType">The entity type (e.g., "connection", "profile", "context").</param>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <param name="fromEntityVersion">The source version to compare from.</param>
    /// <param name="toEntityVersion">The target version to compare to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The differences between the two versions.</returns>
    [HttpGet($"{{{nameof(entityType)}}}/{{{nameof(entityId)}}}/{{{nameof(fromEntityVersion)}}}/compare/{{{nameof(toEntityVersion)}}}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(EntityVersionComparisonResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareVersions(
        [FromRoute] string entityType,
        [FromRoute] Guid entityId,
        [FromRoute] int fromEntityVersion,
        [FromRoute] int toEntityVersion,
        CancellationToken cancellationToken = default)
    {
        // Normalize entity type (case-insensitive)
        var normalizedType = NormalizeEntityType(entityType);
        if (normalizedType is null)
        {
            return UnknownEntityType(entityType);
        }

        var comparison = await _versionService.CompareVersionsAsync(entityId, normalizedType, fromEntityVersion, toEntityVersion, cancellationToken);
        if (comparison is null)
        {
            return NotFound(CreateProblemDetails(
                "Version not found",
                $"One or both versions ({fromEntityVersion}, {toEntityVersion}) were not found for this entity."));
        }

        return Ok(new EntityVersionComparisonResponseModel
        {
            FromVersion = comparison.FromVersion,
            ToVersion = comparison.ToVersion,
            Changes = comparison.Changes.Select(c => new PropertyChangeModel
            {
                PropertyName = c.PropertyName,
                OldValue = c.OldValue,
                NewValue = c.NewValue
            }).ToList()
        });
    }

    /// <summary>
    /// Gets the list of supported entity types.
    /// </summary>
    /// <returns>The list of supported entity type names.</returns>
    [HttpGet("supported-types")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public IActionResult GetSupportedEntityTypes()
    {
        return Ok(_entityTypes.GetSupportedEntityTypes());
    }

    /// <summary>
    /// Rollback an entity to a previous version.
    /// </summary>
    /// <param name="entityType">The entity type (e.g., "connection", "profile", "context").</param>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <param name="entityVersion">The version number to rollback to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>No content on success.</returns>
    [HttpPost($"{{{nameof(entityType)}}}/{{{nameof(entityId)}}}/{{{nameof(entityVersion)}}}/rollback")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RollbackToVersion(
        [FromRoute] string entityType,
        [FromRoute] Guid entityId,
        [FromRoute] int entityVersion,
        CancellationToken cancellationToken = default)
    {
        // Get the entity type handler (case-insensitive)
        var handler = _entityTypes.GetByTypeName(entityType);
        if (handler is null)
        {
            return UnknownEntityType(entityType);
        }

        try
        {
            await handler.RollbackAsync(entityId, entityVersion, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(CreateProblemDetails("Rollback failed", ex.Message));
        }
    }

    private string? NormalizeEntityType(string entityType)
    {
        // Try to find by case-insensitive match
        var handler = _entityTypes.GetByTypeName(entityType);
        return handler?.EntityTypeName;
    }
}
