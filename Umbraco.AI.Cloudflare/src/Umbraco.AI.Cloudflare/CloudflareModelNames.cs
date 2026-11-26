namespace Umbraco.AI.Cloudflare;

/// <summary>
/// Turns Cloudflare model identifiers into names for the model picker.
/// </summary>
internal static class CloudflareModelNames
{
    private const string Prefix = "@cf/";

    /// <summary>
    /// Shows <c>@cf/meta/llama-3.3-70b-instruct-fp8-fast</c> as <c>meta/llama-3.3-70b-instruct-fp8-fast</c>.
    /// Other identifiers (for example <c>@hf/...</c>) are shown as they are.
    /// </summary>
    public static string FormatDisplayName(string modelId)
        => modelId.StartsWith(Prefix, StringComparison.Ordinal) ? modelId[Prefix.Length..] : modelId;
}
