namespace Umbraco.AI.Extensions;

/// <summary>
/// Utility methods for working with Together AI models.
/// Together model ids follow the <c>{org}/{model-name}</c> convention,
/// e.g. <c>meta-llama/Llama-3.3-70B-Instruct-Turbo</c>.
/// </summary>
internal static class TogetherAIModelUtilities
{
    /// <summary>
    /// Formats a Together AI model id into a human-readable display name.
    /// Strips the <c>org/</c> prefix and replaces <c>-</c>/<c>_</c> with spaces.
    /// </summary>
    /// <param name="modelId">The model id (e.g. <c>meta-llama/Llama-3.3-70B-Instruct-Turbo</c>).</param>
    /// <returns>A display name (e.g. <c>Llama 3.3 70B Instruct Turbo</c>).</returns>
    public static string FormatDisplayName(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return modelId;
        }

        var slashIndex = modelId.LastIndexOf('/');
        var name = slashIndex >= 0 && slashIndex < modelId.Length - 1
            ? modelId[(slashIndex + 1)..]
            : modelId;

        return name.Replace('-', ' ').Replace('_', ' ').Trim();
    }
}
