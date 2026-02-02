namespace Umbraco.AI.Core.Models;

/// <summary>
/// A reference to a specific AI model provided by an AI provider.
/// </summary>
/// <param name="providerId"></param>
/// <param name="modelId"></param>
public readonly struct AIModelRef(string providerId, string modelId)
{
    /// <summary>
    /// The alias of the AI provider.
    /// </summary>
    public string ProviderId { get; } = providerId ?? throw new ArgumentNullException(nameof(providerId));
    
    /// <summary>
    /// The unique identifier of the AI model.
    /// </summary>
    public string ModelId { get; } = modelId ?? throw new ArgumentNullException(nameof(modelId));

    /// <summary>
    /// Returns a string representation of the AIModelRef in the format "ProviderId/ModelId".
    /// </summary>
    /// <returns></returns>
    public override string ToString() => $"{ProviderId}/{ModelId}";
}