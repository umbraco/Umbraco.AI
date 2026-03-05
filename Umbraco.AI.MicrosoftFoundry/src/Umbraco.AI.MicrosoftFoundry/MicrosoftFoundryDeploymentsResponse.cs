using System.Text.Json.Serialization;

namespace Umbraco.AI.MicrosoftFoundry;

/// <summary>
/// Response from the Microsoft AI Foundry Deployments API.
/// Endpoint: GET {endpoint}/deployments?api-version=v1
/// </summary>
internal sealed class MicrosoftFoundryDeploymentsResponse
{
    /// <summary>
    /// Gets or sets the list of deployments.
    /// </summary>
    [JsonPropertyName("value")]
    public List<MicrosoftFoundryDeploymentInfo> Value { get; set; } = [];
}

/// <summary>
/// Information about a deployment in Microsoft AI Foundry.
/// </summary>
internal sealed class MicrosoftFoundryDeploymentInfo
{
    /// <summary>
    /// Gets or sets the deployment name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the deployment type (e.g., "Azure.OpenAI", "Azure.AI.Models").
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the deployment properties.
    /// </summary>
    [JsonPropertyName("properties")]
    public MicrosoftFoundryDeploymentProperties? Properties { get; set; }
}

/// <summary>
/// Properties of a deployment.
/// </summary>
internal sealed class MicrosoftFoundryDeploymentProperties
{
    /// <summary>
    /// Gets or sets the model information.
    /// </summary>
    [JsonPropertyName("model")]
    public MicrosoftFoundryDeploymentModel? Model { get; set; }

    /// <summary>
    /// Gets or sets the provisioning state (e.g., "Succeeded", "Failed").
    /// </summary>
    [JsonPropertyName("provisioningState")]
    public string? ProvisioningState { get; set; }
}

/// <summary>
/// Model information within a deployment.
/// </summary>
internal sealed class MicrosoftFoundryDeploymentModel
{
    /// <summary>
    /// Gets or sets the model name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model version.
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the model format (e.g., "OpenAI", "Custom").
    /// </summary>
    [JsonPropertyName("format")]
    public string? Format { get; set; }
}
