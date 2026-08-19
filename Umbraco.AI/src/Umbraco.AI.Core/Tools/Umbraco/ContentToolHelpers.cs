using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.OperationStatus;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Shared helper methods for content tools.
/// </summary>
internal static class ContentToolHelpers
{
    /// <summary>
    /// Builds a breadcrumb path string for a content item (e.g., "Home > About > Team").
    /// </summary>
    /// <param name="content">The published content item.</param>
    /// <returns>A breadcrumb path string.</returns>
    public static string GetContentPath(IPublishedContent content)
    {
        var ancestors = content.Ancestors();
        var pathParts = ancestors.Reverse().Select(a => a.Name).ToList();
        pathParts.Add(content.Name);
        return string.Join(" > ", pathParts);
    }

    /// <summary>
    /// Builds an <see cref="UmbracoContentItem"/> from a published content item,
    /// extracting all properties and parent info.
    /// </summary>
    /// <param name="content">The published content item.</param>
    /// <param name="culture">Optional culture for variant content.</param>
    /// <returns>A fully populated content item.</returns>
    public static UmbracoContentItem BuildContentItem(IPublishedContent content, string? culture = null)
    {
        var properties = PropertyValueFormatter.ExtractProperties(content, culture);

        var parentInfo = content.Parent() is { } parent
            ? new ContentParentItem(parent.Key, parent.Name)
            : null;

        return new UmbracoContentItem(
            content.Key,
            content.Name,
            content.ContentType.Alias,
            content.Url(),
            content.CreateDate,
            content.UpdateDate,
            content.Level,
            GetContentPath(content),
            parentInfo,
            properties);
    }

    /// <summary>
    /// Builds an <see cref="UmbracoContentItem"/> from a draft/business-layer content item — the shape
    /// returned by <c>IContentEditingService.CreateAsync</c>/<c>UpdateAsync</c>, which may not be
    /// published yet. Unlike the <see cref="IPublishedContent"/> overload, this never has a resolvable
    /// URL, and its <c>Path</c> is the raw comma-separated id path (e.g. <c>"-1,1063,1064"</c>) rather
    /// than a friendly breadcrumb — resolving ancestor names would need extra content-service lookups
    /// this static helper has no way to make, and the confirmation payload a write tool returns doesn't
    /// need it. Parent info is omitted (the caller already knows the parent key it just passed in). Use
    /// <see cref="BuildEnrichedContentItem"/> instead for an ad-hoc lookup that doesn't already know these.
    /// </summary>
    /// <param name="content">The draft content item.</param>
    /// <param name="culture">Optional culture for variant content.</param>
    /// <returns>A content item populated from the draft/business-layer model.</returns>
    public static UmbracoContentItem BuildContentItem(IContent content, string? culture = null)
    {
        var properties = ExtractDraftProperties(content, culture);

        return new UmbracoContentItem(
            content.Key,
            content.GetCultureName(culture) ?? content.Name ?? string.Empty,
            content.ContentType.Alias,
            null,
            content.CreateDate,
            content.UpdateDate,
            content.Level,
            content.Path,
            null,
            properties);
    }

    /// <summary>
    /// Builds an <see cref="UmbracoContentItem"/> from a draft/business-layer content item for an ad-hoc
    /// lookup (e.g. <c>get_umbraco_content</c>), where — unlike a write tool — the caller doesn't already
    /// know the item's parent or location. Resolves a friendly breadcrumb and parent info via
    /// <see cref="IContentService.GetAncestors(IContent)"/>, which works regardless of publish state, and
    /// resolves a live <see cref="UmbracoContentItem.Url"/> from the published cache when the item (and
    /// requested culture) is actually published — null otherwise, since an unpublished item has no live URL.
    /// </summary>
    /// <param name="content">The draft content item.</param>
    /// <param name="contentService">Used to resolve ancestors for the breadcrumb and parent info.</param>
    /// <param name="umbracoContextAccessor">Used to resolve a live URL when the item is published.</param>
    /// <param name="culture">Optional culture for variant content.</param>
    /// <returns>A fully populated content item, enriched with breadcrumb/parent/URL where resolvable.</returns>
    public static UmbracoContentItem BuildEnrichedContentItem(
        IContent content,
        IContentService contentService,
        IUmbracoContextAccessor umbracoContextAccessor,
        string? culture = null)
    {
        var properties = ExtractDraftProperties(content, culture);

        var ancestors = contentService.GetAncestors(content).OrderBy(a => a.Level).ToList();

        var parentInfo = ancestors.Count > 0
            ? new ContentParentItem(ancestors[^1].Key, ancestors[^1].GetCultureName(culture) ?? ancestors[^1].Name ?? string.Empty)
            : null;

        var pathParts = ancestors.Select(a => a.GetCultureName(culture) ?? a.Name ?? string.Empty).ToList();
        pathParts.Add(content.GetCultureName(culture) ?? content.Name ?? string.Empty);
        var breadcrumb = string.Join(" > ", pathParts);

        string? url = null;
        if (umbracoContextAccessor.TryGetUmbracoContext(out var umbracoContext))
        {
            url = umbracoContext.Content?.GetById(content.Key)?.Url(culture);
        }

        return new UmbracoContentItem(
            content.Key,
            content.GetCultureName(culture) ?? content.Name ?? string.Empty,
            content.ContentType.Alias,
            url,
            content.CreateDate,
            content.UpdateDate,
            content.Level,
            breadcrumb,
            parentInfo,
            properties);
    }

    /// <summary>
    /// Builds an <see cref="UmbracoMediaItem"/> from a media business-layer model — the shape returned
    /// by <c>IMediaEditingService.CreateAsync</c>/<c>UpdateAsync</c>. Media has no culture/publish
    /// concept, so this is simpler than the content overloads.
    /// </summary>
    /// <param name="media">The media item.</param>
    /// <returns>A media item populated from the business-layer model.</returns>
    public static UmbracoMediaItem BuildMediaItem(IMedia media)
    {
        var properties = ExtractDraftProperties(media, culture: null);

        return new UmbracoMediaItem(
            media.Key,
            media.Name ?? string.Empty,
            media.ContentType.Alias,
            media.CreateDate,
            media.UpdateDate,
            properties);
    }

    /// <summary>
    /// Extracts raw property values from a draft/business-layer content or media item. Unlike
    /// <see cref="PropertyValueFormatter.ExtractProperties(IPublishedContent, string?)"/>, this does
    /// not resolve media pickers/content pickers/blocks into friendly nested shapes — those resolutions
    /// depend on the published cache, which a draft item may never have been through. Values are
    /// returned as stored (e.g. a block property's raw JSON envelope, a media picker's raw UDI list).
    /// </summary>
    private static IReadOnlyList<ContentPropertyItem> ExtractDraftProperties(IContentBase content, string? culture)
    {
        var properties = new List<ContentPropertyItem>();

        foreach (var property in content.Properties)
        {
            properties.Add(new ContentPropertyItem(
                property.Alias,
                property.PropertyType.PropertyEditorAlias,
                property.GetValue(culture)));
        }

        return properties;
    }

    /// <summary>
    /// Maps a content/media editing failure status to a human-readable message for a tool result.
    /// </summary>
    public static string ToMessage(this ContentEditingOperationStatus status) => status switch
    {
        ContentEditingOperationStatus.ContentTypeNotFound => "The specified content type was not found.",
        ContentEditingOperationStatus.ParentNotFound => "The specified parent item was not found.",
        ContentEditingOperationStatus.ParentInvalid => "The specified parent is not valid for this operation.",
        ContentEditingOperationStatus.NotFound => "The specified item was not found.",
        ContentEditingOperationStatus.NotAllowed => "This operation is not allowed here (permission or structural constraint).",
        ContentEditingOperationStatus.PropertyTypeNotFound => "One of the supplied property aliases does not exist on this content type.",
        ContentEditingOperationStatus.PropertyValidationError => "One or more property values failed validation.",
        ContentEditingOperationStatus.InvalidCulture => "The specified culture is invalid or not configured.",
        ContentEditingOperationStatus.InTrash => "This item is in the recycle bin and cannot be edited.",
        ContentEditingOperationStatus.NotInTrash => "This item is not in the recycle bin.",
        ContentEditingOperationStatus.DuplicateKey => "An item with the same key already exists.",
        ContentEditingOperationStatus.DuplicateName => "An item with the same name already exists at this level.",
        ContentEditingOperationStatus.CannotDeleteWhenReferenced => "This item cannot be deleted because it is referenced by other items.",
        ContentEditingOperationStatus.CannotMoveToRecycleBinWhenReferenced => "This item cannot be deleted because it is referenced by other items.",
        ContentEditingOperationStatus.CancelledByNotification => "The operation was cancelled by a notification handler.",
        _ => $"Operation failed: {status}.",
    };

    /// <summary>
    /// Maps a content publishing failure status to a human-readable message for a tool result.
    /// </summary>
    public static string ToMessage(this ContentPublishingOperationStatus status) => status switch
    {
        ContentPublishingOperationStatus.ContentNotFound => "The specified content item was not found.",
        ContentPublishingOperationStatus.ContentInvalid => "This content item is invalid and cannot be published.",
        ContentPublishingOperationStatus.NothingToPublish => "There is nothing to publish for this content item.",
        ContentPublishingOperationStatus.MandatoryCultureMissing => "A mandatory culture is missing from this content item.",
        ContentPublishingOperationStatus.InvalidCulture => "The specified culture is invalid or not configured.",
        ContentPublishingOperationStatus.CultureMissing => "The specified culture is missing from this content item.",
        ContentPublishingOperationStatus.InTrash => "This item is in the recycle bin and cannot be published.",
        ContentPublishingOperationStatus.PathNotPublished => "The parent content must be published before this item can be published.",
        ContentPublishingOperationStatus.UnsavedChanges => "This item has unsaved changes that must be saved before publishing.",
        ContentPublishingOperationStatus.CannotUnpublishWhenReferenced => "This item cannot be unpublished because it is referenced by other items.",
        ContentPublishingOperationStatus.CancelledByEvent => "The operation was cancelled by an event handler.",
        _ => $"Operation failed: {status}.",
    };
}

/// <summary>
/// A media item, as returned by the media write tools.
/// </summary>
/// <param name="Key">The unique key of the media item.</param>
/// <param name="Name">The name of the media item.</param>
/// <param name="MediaType">The media type alias.</param>
/// <param name="CreateDate">The creation date.</param>
/// <param name="UpdateDate">The last update date.</param>
/// <param name="Properties">The media item's properties with their raw stored values.</param>
public record UmbracoMediaItem(
    Guid Key,
    string Name,
    string MediaType,
    DateTime CreateDate,
    DateTime UpdateDate,
    IReadOnlyList<ContentPropertyItem> Properties);
