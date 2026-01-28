using System.ComponentModel.DataAnnotations;
using Umbraco.Ai.Core.EditableModels;

namespace Umbraco.Ai.Amazon;

/// <summary>
/// Settings for the Amazon Bedrock provider.
/// </summary>
public class AmazonProviderSettings
{
    /// <summary>
    /// The AWS region for Bedrock services (e.g., "us-east-1").
    /// </summary>
    [AiField(DefaultValue = "us-east-1")]
    [Required]
    public string? Region { get; set; }

    /// <summary>
    /// The AWS Access Key ID for authenticating with Bedrock services.
    /// </summary>
    [AiField(IsSensitive = true)]
    [Required]
    public string? AccessKeyId { get; set; }

    /// <summary>
    /// The AWS Secret Access Key for authenticating with Bedrock services.
    /// </summary>
    [AiField(IsSensitive = true)]
    [Required]
    public string? SecretAccessKey { get; set; }

    /// <summary>
    /// Custom endpoint URL for Bedrock services (optional).
    /// </summary>
    [AiField]
    public string? Endpoint { get; set; }
}
