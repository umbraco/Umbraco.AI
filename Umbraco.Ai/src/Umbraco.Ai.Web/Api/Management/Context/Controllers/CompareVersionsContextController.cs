using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Contexts;
using Umbraco.Ai.Extensions;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Common.Models;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Context.Controllers;

/// <summary>
/// Controller to compare two versions of a context.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class CompareVersionsContextController : ContextControllerBase
{
    private readonly IAiContextService _contextService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompareVersionsContextController"/> class.
    /// </summary>
    public CompareVersionsContextController(IAiContextService contextService)
    {
        _contextService = contextService;
    }

    /// <summary>
    /// Compare two versions of a context.
    /// </summary>
    /// <param name="contextIdOrAlias">The unique identifier (GUID) or alias of the context.</param>
    /// <param name="fromVersion">The source version to compare from.</param>
    /// <param name="toVersion">The target version to compare to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The differences between the two versions.</returns>
    [HttpGet($"{{{nameof(contextIdOrAlias)}}}/versions/compare")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(VersionComparisonResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompareVersions(
        [FromRoute] IdOrAlias contextIdOrAlias,
        [FromQuery] int fromVersion,
        [FromQuery] int toVersion,
        CancellationToken cancellationToken = default)
    {
        var context = await _contextService.GetContextAsync(contextIdOrAlias, cancellationToken);
        if (context is null)
        {
            return ContextNotFound();
        }

        // Get the "from" version - this could be a historical snapshot or the current version
        var fromSnapshot = fromVersion == context.Version
            ? context
            : await _contextService.GetContextVersionSnapshotAsync(context.Id, fromVersion, cancellationToken);

        if (fromSnapshot is null)
        {
            return NotFound(CreateProblemDetails(
                "Version not found",
                $"Version {fromVersion} was not found for this context."));
        }

        // Get the "to" version - this could be a historical snapshot or the current version
        var toSnapshot = toVersion == context.Version
            ? context
            : await _contextService.GetContextVersionSnapshotAsync(context.Id, toVersion, cancellationToken);

        if (toSnapshot is null)
        {
            return NotFound(CreateProblemDetails(
                "Version not found",
                $"Version {toVersion} was not found for this context."));
        }

        var changes = CompareContexts(fromSnapshot, toSnapshot);

        return Ok(new VersionComparisonResponseModel
        {
            FromVersion = fromVersion,
            ToVersion = toVersion,
            Changes = changes
        });
    }

    private static List<PropertyChangeModel> CompareContexts(AiContext from, AiContext to)
    {
        var changes = new List<PropertyChangeModel>();

        // Compare Name
        if (from.Name != to.Name)
        {
            changes.Add(new PropertyChangeModel
            {
                PropertyName = "Name",
                OldValue = from.Name,
                NewValue = to.Name
            });
        }

        // Compare Alias
        if (from.Alias != to.Alias)
        {
            changes.Add(new PropertyChangeModel
            {
                PropertyName = "Alias",
                OldValue = from.Alias,
                NewValue = to.Alias
            });
        }

        // Compare Resources (as JSON)
        var fromResources = SerializeResources(from.Resources);
        var toResources = SerializeResources(to.Resources);
        if (fromResources != toResources)
        {
            changes.Add(new PropertyChangeModel
            {
                PropertyName = "Resources",
                OldValue = fromResources,
                NewValue = toResources
            });
        }

        return changes;
    }

    private static string SerializeResources(IList<AiContextResource> resources)
    {
        if (resources.Count == 0)
        {
            return "";
        }

        // Serialize only the relevant properties for comparison (excluding IDs)
        var resourceData = resources
            .OrderBy(r => r.SortOrder)
            .Select(r => new
            {
                r.ResourceTypeId,
                r.Name,
                r.Description,
                r.SortOrder,
                r.Data,
                r.InjectionMode
            });

        return JsonSerializer.Serialize(resourceData, new JsonSerializerOptions { WriteIndented = true });
    }
}
