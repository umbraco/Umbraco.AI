using Umbraco.AI.Core.Settings;
using Umbraco.AI.Web.Api.Management.Settings.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Settings.Mapping;

/// <summary>
/// Defines mappings for Settings API models.
/// </summary>
public class SettingsMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        // Domain -> Response
        mapper.Define<AISettings, SettingsResponseModel>((_, _) => new SettingsResponseModel(), MapToResponse);

        // Request -> Domain
        mapper.Define<UpdateSettingsRequestModel, AISettings>((_, _) => new AISettings(), MapFromUpdateRequest);
    }

    // Umbraco.Code.MapAll
    private static void MapToResponse(AISettings source, SettingsResponseModel target, MapperContext context)
    {
        target.DefaultChatProfileId = source.DefaultChatProfileId;
        target.DefaultEmbeddingProfileId = source.DefaultEmbeddingProfileId;
        target.ClassifierChatProfileId = source.ClassifierChatProfileId;
        target.DefaultSpeechToTextProfileId = source.DefaultSpeechToTextProfileId;
        target.DefaultImageGenerationProfileId = source.DefaultImageGenerationProfileId;
        target.DefaultDecisionProfileId = source.DefaultDecisionProfileId;
        target.DisclosureNoticeMode = source.DisclosureNoticeMode.ToString();
    }

    // Umbraco.Code.MapAll -DateCreated -CreatedByUserId -DateModified -ModifiedByUserId
    private static void MapFromUpdateRequest(UpdateSettingsRequestModel source, AISettings target, MapperContext context)
    {
        target.DefaultChatProfileId = source.DefaultChatProfileId;
        target.DefaultEmbeddingProfileId = source.DefaultEmbeddingProfileId;
        target.ClassifierChatProfileId = source.ClassifierChatProfileId;
        target.DefaultSpeechToTextProfileId = source.DefaultSpeechToTextProfileId;
        target.DefaultImageGenerationProfileId = source.DefaultImageGenerationProfileId;
        target.DefaultDecisionProfileId = source.DefaultDecisionProfileId;
        target.DisclosureNoticeMode = ParseDisclosureNoticeMode(source.DisclosureNoticeMode);
    }

    /// <summary>
    /// Parses a disclosure notice mode, falling back to <see cref="AIDisclosureNoticeMode.Always"/> so a
    /// missing or bad value never silently turns the notice off.
    /// </summary>
    internal static AIDisclosureNoticeMode ParseDisclosureNoticeMode(string? value)
        => Enum.TryParse<AIDisclosureNoticeMode>(value, true, out var mode) && Enum.IsDefined(mode)
            ? mode
            : AIDisclosureNoticeMode.Always;
}
