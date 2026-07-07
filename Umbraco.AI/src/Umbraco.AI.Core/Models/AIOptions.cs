namespace Umbraco.AI.Core.Models;

/// <summary>
/// Configuration options for AI services.
/// </summary>
public class AIOptions
{
    /// <summary>
    /// The default chat profile alias to use when none is specified.
    /// </summary>
    public string? DefaultChatProfileAlias { get; set; }

    /// <summary>
    /// The default embedding profile alias to use when none is specified.
    /// </summary>
    public string? DefaultEmbeddingProfileAlias { get; set; }

    /// <summary>
    /// The classifier chat profile alias to use for internal classification tasks when none is specified.
    /// </summary>
    public string? ClassifierChatProfileAlias { get; set; }

    /// <summary>
    /// The default speech-to-text profile alias to use when none is specified.
    /// </summary>
    public string? DefaultSpeechToTextProfileAlias { get; set; }

    /// <summary>
    /// The default image-generation profile alias to use when none is specified.
    /// </summary>
    public string? DefaultImageGenerationProfileAlias { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="Microsoft.Extensions.Configuration.IConfiguration"/> key
    /// prefixes that editable model settings may dereference via the <c>$Key:Path</c> syntax.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Settings fields support a <c>$Key:Path</c> syntax that resolves to a configuration
    /// value at run time (e.g. <c>$Umbraco:AI:Secrets:ApiKey</c>), letting admins keep
    /// credentials and per-environment values in app settings / environment variables rather
    /// than the database.
    /// </para>
    /// <para>
    /// Configuration references are resolved server-side, so without a restriction a settings
    /// author could reference <em>any</em> configuration value, not just AI-related ones. To
    /// keep references confined to sanctioned values, resolution is <strong>default-deny</strong>:
    /// a key is only resolvable when it falls under one of these prefixes. The allow-list lives
    /// here (app settings) by design, so only someone who already has access to the
    /// configuration decides which subset settings may reference.
    /// </para>
    /// <para>
    /// Defaults to two dedicated sections, mirroring the GitHub Actions split:
    /// <c>Umbraco:AI:Secrets</c> for sensitive values and <c>Umbraco:AI:Variables</c> for
    /// non-sensitive per-environment values. Place AI-facing values under these. Add further
    /// prefixes to expose existing configuration sections without copying them, accepting that
    /// everything under an added prefix becomes readable by anyone who can edit settings.
    /// Matching is segment-aware and case-insensitive: the prefix <c>Umbraco:AI:Secrets</c>
    /// permits <c>Umbraco:AI:Secrets:ApiKey</c> but not <c>Umbraco:AI:SecretsBackup:X</c>.
    /// </para>
    /// <para>
    /// Keys under a <see cref="SecretConfigurationKeyPrefixes"/> prefix carry the extra
    /// restriction that they may only be referenced from sensitive fields — see that property.
    /// </para>
    /// </remarks>
    public IList<string> AllowedConfigurationKeyPrefixes { get; set; } = new List<string>
    {
        "Umbraco:AI:Secrets",
        "Umbraco:AI:Variables",
    };

    /// <summary>
    /// Gets or sets the subset of <see cref="AllowedConfigurationKeyPrefixes"/> whose values
    /// are treated as secret, and which may therefore only be referenced from settings fields
    /// marked <c>[AIField(IsSensitive = true)]</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The allow-list confines references to the sanctioned sections; this adds that a value
    /// resolved from a secret key should only land in a field the system treats as
    /// credential-bearing (encrypted at rest, masked in the UI) rather than a clear-text
    /// field. Restricting secret keys to sensitive fields enforces that: a secret resolves
    /// only where a directly-entered credential would belong.
    /// </para>
    /// <para>
    /// Mirrors the GitHub Actions split: <c>Umbraco:AI:Secrets</c> (the default here) is
    /// sensitive and field-restricted; <c>Umbraco:AI:Variables</c> is left off this list so
    /// non-secret per-environment values (base URLs, IDs, flags) can be referenced from any
    /// field. Entries should be a subset of <see cref="AllowedConfigurationKeyPrefixes"/>; a
    /// secret prefix that is not also allowed is simply never resolvable. Matching is
    /// segment-aware and case-insensitive, as for the allow-list.
    /// </para>
    /// </remarks>
    public IList<string> SecretConfigurationKeyPrefixes { get; set; } = new List<string>
    {
        "Umbraco:AI:Secrets",
    };

    // TODO: public string? DefaultModerationProviderAlias { get; set; }
    // TODO: public string? DefaultToolProviderAlias { get; set; }
}