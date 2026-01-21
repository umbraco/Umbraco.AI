using System.ComponentModel;
using Umbraco.Ai.Core.RuntimeContext;
using Umbraco.Ai.Prompt.Core.Media;

namespace Umbraco.Ai.Core.Tools.Umbraco;


/// <summary>
/// Arguments for retrieving a media item from Umbraco.
/// </summary>
/// <param name="MediaKey"></param>
/// <param name="Description"></param>
public sealed record GetUmbracoMediaItemArgs(
    [property: Description("The unique key identifier of the media item")]
    Guid MediaKey,

    [property: Description("A description for the media item to be used in analysis")]
    string Description);

/// <summary>
/// Tool that retrieves a media item from Umbraco by its unique key.
/// </summary>
/// <param name="runtimeContextAccessor"></param>
/// <param name="mediaResolver"></param>
[AiTool("get_umbraco_media", "Retrieves a media item from Umbraco")]
public class GetUmbracoMediaItem(IAiRuntimeContextAccessor runtimeContextAccessor, IAiUmbracoMediaResolver mediaResolver) : AiToolBase<GetUmbracoMediaItemArgs>
{
    /// <inheritdoc />
    public override string Description =>
        "Retrieves a media item from Umbraco by its unique key. " +
        "The media item is prepared for analysis by adding its binary data and media type to the runtime context. " +
        "Use this tool to fetch images or other media for further processing or analysis.";
    
    /// <inheritdoc />
    protected override async Task<object> ExecuteAsync(GetUmbracoMediaItemArgs args, CancellationToken ct)
    {
        var media = await mediaResolver.ResolveAsync(args.MediaKey, ct);
        if (media is null)
        {
            return new { success = false, message = "Media item could not be found or is not a valid media type" };
        }
        runtimeContextAccessor.Context?.AddData(media.Data, media.MediaType, args.Description);
        return new { success = true, message = "Image ready for analysis" };
    }
}