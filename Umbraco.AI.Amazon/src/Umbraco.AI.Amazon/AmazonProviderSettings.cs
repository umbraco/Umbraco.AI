using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Amazon;

/// <summary>
/// Settings for the Amazon Bedrock provider.
/// </summary>
public class AmazonProviderSettings
{
    /// <summary>
    /// The AWS region for Bedrock services (e.g., "us-east-1").
    /// </summary>
    [AIField]
    [Required]
    public string? Region { get; set; } = "us-east-1";

    /// <summary>
    /// The AWS Access Key ID for authenticating with Bedrock services.
    /// </summary>
    [AIField(IsSensitive = true)]
    [Required]
    public string? AccessKeyId { get; set; }

    /// <summary>
    /// The AWS Secret Access Key for authenticating with Bedrock services.
    /// </summary>
    [AIField(IsSensitive = true)]
    [Required]
    public string? SecretAccessKey { get; set; }

    /// <summary>
    /// Custom endpoint URL for Bedrock services (optional).
    /// </summary>
    [AIField]
    public string? Endpoint { get; set; }
}
