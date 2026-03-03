namespace Umbraco.AI.Deploy.Configuration;

/// <summary>
/// Configuration settings for Umbraco.AI Deploy.
/// </summary>
public class UmbracoAIDeploySettings
{
    /// <summary>
    /// Connection deployment settings (filtering sensitive data).
    /// </summary>
    public UmbracoAIDeployConnectionSettings Connections { get; set; } = new();
}

/// <summary>
/// Configuration settings for AI Connection deployment.
/// Controls how sensitive settings are filtered during deployment.
/// </summary>
public class UmbracoAIDeployConnectionSettings
{
    /// <summary>
    /// If true, ignore fields with encrypted values (values starting with "ENC:").
    /// ALLOWS: $ configuration references (e.g., "$OpenAI:ApiKey")
    /// BLOCKS: Encrypted values (ENC:...)
    /// </summary>
    public bool IgnoreEncrypted { get; set; } = true;

    /// <summary>
    /// If true, ignore fields marked with [AIField(IsSensitive = true)] attribute.
    /// BLOCKS: All values from sensitive fields, even $ configuration references
    /// Most restrictive - use for fields that should never be deployed.
    /// </summary>
    public bool IgnoreSensitive { get; set; } = false;

    /// <summary>
    /// Specific settings field names to always ignore during deployment.
    /// BLOCKS: All values for these specific fields (most specific control)
    /// Use this for fine-grained control over individual fields.
    /// Takes precedence over IgnoreEncrypted and IgnoreSensitive.
    /// </summary>
    public string[] IgnoreSettings { get; set; } = [];
}
