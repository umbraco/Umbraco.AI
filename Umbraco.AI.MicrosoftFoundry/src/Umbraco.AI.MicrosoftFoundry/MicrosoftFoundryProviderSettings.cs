using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.MicrosoftFoundry;

/// <summary>
/// Settings for the Microsoft AI Foundry provider.
/// </summary>
/// <remarks>
/// Supports two authentication methods:
/// <list type="bullet">
/// <item><description><b>Entra ID</b>: Azure AD authentication using a service principal (TenantId + ClientId + ClientSecret)
/// or managed identity / DefaultAzureCredential (TenantId only or no Entra ID fields).
/// When Entra ID is configured, the provider can list deployed models via the deployments API.</description></item>
/// <item><description><b>API Key</b>: Simple authentication using an API key. Deprecated.</description></item>
/// </list>
/// </remarks>
public class MicrosoftFoundryProviderSettings
{
    /// <summary>
    /// The Microsoft AI Foundry endpoint URL.
    /// </summary>
    /// <remarks>
    /// Example: https://your-resource.services.ai.azure.com/
    /// </remarks>
    [AIField]
    [Required]
    public string? Endpoint { get; set; }

    /// <summary>
    /// The AI Foundry project name. Required for Entra ID authentication to list deployed models.
    /// </summary>
    /// <remarks>
    /// Found in the AI Foundry portal under project settings.
    /// Used to build the project-scoped endpoint: {Endpoint}/api/projects/{ProjectName}/deployments
    /// </remarks>
    [AIField(Group = "EntraId")]
    public string? ProjectName { get; set; }

    /// <summary>
    /// The Azure AD tenant ID for Entra ID authentication.
    /// </summary>
    [AIField(Group = "EntraId")]
    public string? TenantId { get; set; }

    /// <summary>
    /// The client (application) ID for Entra ID service principal authentication.
    /// </summary>
    [AIField(Group = "EntraId")]
    public string? ClientId { get; set; }

    /// <summary>
    /// The client secret for Entra ID service principal authentication.
    /// </summary>
    [AIField(IsSensitive = true, Group = "EntraId")]
    public string? ClientSecret { get; set; }

    /// <summary>
    /// The API key for authenticating with Microsoft AI Foundry services.
    /// Optional when using Entra ID authentication.
    /// </summary>
    [AIField(IsSensitive = true, Group = "ApiKey")]
    public string? ApiKey { get; set; }
}
