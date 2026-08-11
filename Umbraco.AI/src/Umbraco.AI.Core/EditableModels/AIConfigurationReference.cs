namespace Umbraco.AI.Core.EditableModels;

/// <summary>
/// The single definition of what makes a settings value a configuration reference.
/// </summary>
/// <remarks>
/// A value starting with <c>$</c> normally names a configuration key to dereference
/// (<c>$Umbraco:AI:Secrets:ApiKey</c>). A value starting with <c>$$</c> is the escape hatch for a
/// literal that happens to begin with <c>$</c> — <c>$$foo</c> resolves to the literal <c>$foo</c>.
///
/// The distinction matters beyond resolution: a reference is a pointer and safe to store and display
/// as-is, while an escaped literal in a sensitive field is a real secret and must be treated like any
/// other. Getting that wrong stored <c>$$mysecret</c> in plaintext, so the rule lives here rather than
/// being restated at each call site.
/// </remarks>
internal static class AIConfigurationReference
{
    /// <summary>
    /// The prefix marking a value as a configuration reference.
    /// </summary>
    internal const string Prefix = "$";

    /// <summary>
    /// The prefix marking a value as an escaped literal rather than a reference.
    /// </summary>
    internal const string EscapedPrefix = "$$";

    /// <summary>
    /// Determines whether <paramref name="value"/> dereferences a configuration key.
    /// Escaped literals (<c>$$...</c>) are not references.
    /// </summary>
    internal static bool IsReference(string? value)
        => value is not null
           && value.StartsWith(Prefix, StringComparison.Ordinal)
           && !value.StartsWith(EscapedPrefix, StringComparison.Ordinal);

    /// <summary>
    /// Determines whether <paramref name="value"/> is an escaped literal — a value the author wants
    /// taken verbatim even though it starts with <c>$</c>. Use <see cref="Unescape"/> to read it.
    /// </summary>
    internal static bool IsEscapedLiteral(string? value)
        => value is not null && value.StartsWith(EscapedPrefix, StringComparison.Ordinal);

    /// <summary>
    /// Strips the escape from an escaped literal, so <c>$$foo</c> becomes <c>$foo</c>.
    /// </summary>
    internal static string Unescape(string value) => value[1..];
}
