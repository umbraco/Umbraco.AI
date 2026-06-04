namespace Umbraco.AI.Core.Telemetry;

/// <summary>
/// Normalizes model IDs to well-known public model family names for usage telemetry.
/// </summary>
/// <remarks>
/// Model IDs can be user-authored (e.g., Azure AI Foundry deployment names, self-hosted model
/// names) and may encode business information, so raw values must never be reported.
/// Instead, the ID is matched against a list of public model family tokens; anything that
/// doesn't match reports as "other".
/// </remarks>
internal static class AIModelFamilyNormalizer
{
    /// <summary>
    /// Known public model family tokens, ordered most-specific first.
    /// Matching is token-boundary aware, so "gpt-4" will not match inside "gpt-4o".
    /// </summary>
    private static readonly string[] _familyTokens =
    [
        // OpenAI
        "gpt-5", "gpt-4.1", "gpt-4o", "gpt-4", "gpt-3.5",
        "o1", "o3", "o4",
        "dall-e", "whisper", "sora",
        "text-embedding-3", "text-embedding-ada",
        // Anthropic
        "claude-opus", "claude-sonnet", "claude-haiku", "claude",
        // Google
        "gemini", "gemma", "imagen", "veo",
        // Mistral
        "magistral", "ministral", "mixtral", "mistral", "codestral", "pixtral", "devstral", "voxtral",
        // DeepSeek
        "deepseek",
        // Amazon
        "nova", "titan",
        // Open-weight families (TogetherAI, FireworksAI, HuggingFace hosts)
        "llama", "qwen", "phi", "kimi", "glm", "grok", "command", "jamba",
        "stable-diffusion", "flux",
    ];

    /// <summary>
    /// Normalizes a model reference to "{providerId}/{family}", or "{providerId}/other"
    /// when the model ID doesn't match a known public model family.
    /// </summary>
    public static string Normalize(string providerId, string modelId)
    {
        var normalizedProvider = providerId.ToLowerInvariant();
        var normalizedModel = modelId.ToLowerInvariant();

        foreach (var token in _familyTokens)
        {
            if (ContainsToken(normalizedModel, token))
            {
                return $"{normalizedProvider}/{token}";
            }
        }

        return $"{normalizedProvider}/other";
    }

    /// <summary>
    /// Checks whether <paramref name="value"/> contains <paramref name="token"/> bounded by
    /// non-alphanumeric characters (or string edges), so "gpt-4" doesn't match inside "gpt-4o".
    /// </summary>
    private static bool ContainsToken(string value, string token)
    {
        var index = 0;
        while ((index = value.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            var beforeOk = index == 0 || !char.IsLetterOrDigit(value[index - 1]);
            var afterIndex = index + token.Length;
            var afterOk = afterIndex >= value.Length || !char.IsLetterOrDigit(value[afterIndex]);

            if (beforeOk && afterOk)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }
}
