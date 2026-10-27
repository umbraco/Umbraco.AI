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
/// Controls which settings are written to, and read from, deploy artifacts.
/// </summary>
/// <remarks>
/// Sensitive fields (those the provider marks <c>[AIField(IsSensitive = true)]</c>) are never
/// written to or read from an artifact as a literal value, unless listed in
/// <see cref="IncludeSensitiveSettings"/>. Configuration references (<c>$Umbraco:AI:Secrets:...</c>)
/// are always deployed, since they only name a setting. Encrypted values (<c>ENC:...</c>) are never
/// deployed, since each environment has its own encryption keys.
/// </remarks>
public class UmbracoAIDeployConnectionSettings
{
    /// <summary>
    /// No longer used. Encrypted values (values starting with "ENC:") are never deployed.
    /// </summary>
    [Obsolete("Encrypted values are never deployed. This setting has no effect. Will be removed in v20.")]
    public bool IgnoreEncrypted { get; set; } = true;

    /// <summary>
    /// No longer used. Sensitive fields are never deployed as literal values; use
    /// <see cref="IncludeSensitiveSettings"/> to opt specific fields in.
    /// </summary>
    [Obsolete("Sensitive fields are never deployed as literal values. This setting has no effect. Use IncludeSensitiveSettings to opt fields in. Will be removed in v20.")]
    public bool IgnoreSensitive { get; set; } = false;

    /// <summary>
    /// Settings field names to always ignore during deployment, whatever their value
    /// (configuration references included). Matched case-insensitively.
    /// Takes precedence over <see cref="IncludeSensitiveSettings"/>.
    /// </summary>
    public string[] IgnoreSettings { get; set; } = [];

    /// <summary>
    /// Sensitive settings field names whose literal values may be deployed. Matched case-insensitively.
    /// </summary>
    /// <remarks>
    /// Values of these fields are written in plain text to the deploy artifact on disk, and so to
    /// source control. Prefer a configuration reference (<c>$Umbraco:AI:Secrets:...</c>) instead.
    /// </remarks>
    public string[] IncludeSensitiveSettings { get; set; } = [];
}
