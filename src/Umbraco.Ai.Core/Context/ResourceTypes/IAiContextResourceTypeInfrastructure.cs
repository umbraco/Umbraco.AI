using Umbraco.Cms.Core.Serialization;

namespace Umbraco.Ai.Core.Context.ResourceTypes;

/// <summary>
/// Defines the infrastructure components required by AI context resource types.
/// </summary>
public interface IAiContextResourceTypeInfrastructure
{
    /// <summary>
    /// JSON serializer for deserializing resource data.
    /// </summary>
    IJsonSerializer JsonSerializer { get; }
}
