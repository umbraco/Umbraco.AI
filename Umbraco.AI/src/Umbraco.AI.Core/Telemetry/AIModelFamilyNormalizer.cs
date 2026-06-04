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
    /// Known public model family tokens mapped to the reported family name, ordered
    /// most-specific first. Matching is token-boundary aware, so "gpt-4" will not match
    /// inside "gpt-4o".
    /// </summary>
    private static readonly (string Token, string Family)[] _familyTokens =
    [
        // OpenAI
        ("gpt-5", "gpt-5"), ("gpt-4.1", "gpt-4.1"), ("gpt-4o", "gpt-4o"), ("gpt-4", "gpt-4"), ("gpt-3.5", "gpt-3.5"),
        ("o1", "o1"), ("o3", "o3"), ("o4", "o4"),
        ("dall-e", "dall-e"), ("whisper", "whisper"), ("sora", "sora"),
        ("text-embedding-3", "text-embedding-3"), ("text-embedding-ada", "text-embedding-ada"),
        // Anthropic - tier tokens match standalone so both "claude-sonnet-4-5" and the older
        // "claude-3-5-sonnet" naming normalize to the same family
        ("opus", "claude-opus"), ("sonnet", "claude-sonnet"), ("haiku", "claude-haiku"), ("claude", "claude"),
        // Google
        ("gemini", "gemini"), ("gemma", "gemma"), ("imagen", "imagen"), ("veo", "veo"),
        // Mistral
        ("magistral", "magistral"), ("ministral", "ministral"), ("mixtral", "mixtral"), ("mistral", "mistral"),
        ("codestral", "codestral"), ("pixtral", "pixtral"), ("devstral", "devstral"), ("voxtral", "voxtral"),
        // DeepSeek
        ("deepseek", "deepseek"),
        // Amazon
        ("nova", "nova"), ("titan", "titan"),
        // Open-weight families (TogetherAI, FireworksAI, HuggingFace hosts)
        ("llama", "llama"), ("qwen", "qwen"), ("phi", "phi"), ("kimi", "kimi"), ("glm", "glm"),
        ("grok", "grok"), ("command", "command"), ("jamba", "jamba"),
        ("stable-diffusion", "stable-diffusion"), ("flux", "flux"),
    ];

    /// <summary>
    /// Normalizes a model reference to "{providerId}/{family}", or "{providerId}/other"
    /// when the model ID doesn't match a known public model family.
    /// </summary>
    public static string Normalize(string providerId, string modelId)
    {
        var normalizedProvider = providerId.ToLowerInvariant();
        var normalizedModel = modelId.ToLowerInvariant();

        foreach ((var token, var family) in _familyTokens)
        {
            if (ContainsToken(normalizedModel, token))
            {
                return $"{normalizedProvider}/{family}";
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
