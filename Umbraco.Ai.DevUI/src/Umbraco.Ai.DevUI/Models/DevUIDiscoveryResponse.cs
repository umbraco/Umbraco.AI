using System.Text.Json.Serialization;

namespace Umbraco.Ai.DevUI.Models;

/// <summary>
/// Response containing discovered entities for DevUI.
/// </summary>
public record DevUIDiscoveryResponse(
    [property: JsonPropertyName("entities")] List<DevUIEntityInfo> Entities);
