using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Registry;

namespace Umbraco.Ai.Extensions;

/// <summary>
/// Extension methods for <see cref="IAiRegistry"/>.
/// </summary>
public static class AiRegistryExtensions
{
    /// <summary>
    /// Gets all AI models for a given provider that support a specific capability.
    /// </summary>
    /// <param name="registry"></param>
    /// <param name="providerId"></param>
    /// <param name="capability"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static async Task<IReadOnlyCollection<AiModelDescriptor>> GetModelsByCapabilityAsync(this IAiRegistry registry, string providerId, AiCapability capability, CancellationToken cancellationToken = default)
    {
        var provider = registry.GetProvider(providerId);
        if (provider == null)
        {
            throw new InvalidOperationException($"AI Provider with ID '{providerId}' not found.");
        }
        
        // TODO: Handle settings (Need a connection)
        
        var modelTasks = provider.GetCapabilities().Where(x => x.Kind == capability)
            .Select(x => x.GetModelsAsync(cancellationToken: cancellationToken));
        var models = await Task.WhenAll(modelTasks);
        
        return models.SelectMany(x => x).ToList().AsReadOnly();
    }
}