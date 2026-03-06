using System.Text.Json.Serialization;

namespace Umbraco.AI.MicrosoftFoundry;

/// <summary>
/// Response from the Microsoft AI Foundry Deployments API.
/// Endpoint: GET {endpoint}/api/projects/{project}/deployments?api-version=v1
/// </summary>
internal sealed class MicrosoftFoundryDeploymentsResponse
{
    [JsonPropertyName("value")]
    public List<MicrosoftFoundryDeploymentInfo> Value { get; set; } = [];
}

/// <summary>
/// Information about a deployment in Microsoft AI Foundry.
/// </summary>
internal sealed class MicrosoftFoundryDeploymentInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("modelName")]
    public string? ModelName { get; set; }

    [JsonPropertyName("modelVersion")]
    public string? ModelVersion { get; set; }

    [JsonPropertyName("modelPublisher")]
    public string? ModelPublisher { get; set; }
}
