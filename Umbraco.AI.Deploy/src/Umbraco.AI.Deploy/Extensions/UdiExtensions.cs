using Umbraco.AI.Core.Models;
using Umbraco.Cms.Core;

namespace Umbraco.AI.Deploy.Extensions;

/// <summary>
/// Extension methods for creating UDIs from AI entities.
/// </summary>
internal static class UdiExtensions
{
    /// <summary>
    /// Creates a GuidUdi for an AI entity with the specified entity type.
    /// </summary>
    public static GuidUdi GetUdi(this IAIEntity entity, string udiEntityType)
        => new(udiEntityType, entity.Id);
}
