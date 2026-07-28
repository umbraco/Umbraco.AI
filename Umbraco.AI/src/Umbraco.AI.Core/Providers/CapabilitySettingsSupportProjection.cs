using Umbraco.AI.Core.Models;
using Umbraco.AI.Extensions;

namespace Umbraco.AI.Core.Providers;

/// <summary>
/// Projects a capability's per-model setting declarations into the metadata carried by each
/// <see cref="AIModelDescriptor"/>, so the model list the backoffice already fetches also says which
/// settings apply to each model.
/// </summary>
/// <remarks>
/// Applied by the capability bases around the provider's own <c>GetModelsAsync</c>, which keeps
/// providers free of the metadata convention — they implement
/// <see cref="IAICapability.GetSettingsSupport"/> and nothing else.
/// </remarks>
internal static class CapabilitySettingsSupportProjection
{
    internal static IReadOnlyList<AIModelDescriptor> Apply(
        IAICapability capability,
        IReadOnlyList<AIModelDescriptor> models)
    {
        if (models.Count == 0)
        {
            return models;
        }

        List<AIModelDescriptor>? projected = null;

        for (var i = 0; i < models.Count; i++)
        {
            var model = models[i];
            var support = capability.GetSettingsSupport(model.Model.ModelId);
            if (support.IsEmpty)
            {
                projected?.Add(model);
                continue;
            }

            // First declaration seen — copy everything already walked past, then continue with the copy.
            if (projected is null)
            {
                projected = new List<AIModelDescriptor>(models.Count);
                projected.AddRange(models.Take(i));
            }

            projected.Add(model.WithMetadata(support.ToMetadata()));
        }

        return projected ?? models;
    }
}
