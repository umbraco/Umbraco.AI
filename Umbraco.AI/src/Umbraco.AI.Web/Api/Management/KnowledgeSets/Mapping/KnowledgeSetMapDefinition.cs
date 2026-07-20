using Umbraco.AI.Core.Contexts.KnowledgeSets;
using Umbraco.AI.Web.Api.Management.KnowledgeSets.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.KnowledgeSets.Mapping;

/// <summary>
/// Map definitions for Knowledge Set models.
/// </summary>
public class KnowledgeSetMapDefinition : IMapDefinition
{
    /// <summary>
    /// The <see cref="MapperContext.Items"/> key used to pass the resolved item count into the mapper.
    /// </summary>
    /// <remarks>
    /// <see cref="IAIKnowledgeSet.GetItemsAsync"/> is async, but <see cref="IUmbracoMapper"/> map actions are
    /// synchronous. Callers must await <see cref="IAIKnowledgeSet.GetItemsAsync"/> themselves and pass the
    /// resolved count through the mapper context using this key, keeping this map definition synchronous.
    /// </remarks>
    internal const string ItemCountKey = "KnowledgeSetItemCount";

    /// <summary>
    /// The <see cref="MapperContext.Items"/> key used to pass the resolved items into the mapper.
    /// </summary>
    /// <remarks>
    /// <see cref="IAIKnowledgeSet.GetItemsAsync"/> is async, but <see cref="IUmbracoMapper"/> map actions are
    /// synchronous. Callers must await <see cref="IAIKnowledgeSet.GetItemsAsync"/> themselves and pass the
    /// resolved items through the mapper context using this key, keeping this map definition synchronous.
    /// </remarks>
    internal const string ItemsKey = "KnowledgeSetItems";

    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        // Knowledge set mappings
        mapper.Define<IAIKnowledgeSet, KnowledgeSetResponseModel>((_, _) => new KnowledgeSetResponseModel
        {
            Id = string.Empty,
            Name = string.Empty
        }, MapKnowledgeSetToResponse);

        mapper.Define<IAIKnowledgeSet, KnowledgeSetDetailResponseModel>((_, _) => new KnowledgeSetDetailResponseModel
        {
            Id = string.Empty,
            Name = string.Empty
        }, MapKnowledgeSetToDetailResponse);

        mapper.Define<AIKnowledgeSetItem, KnowledgeSetItemModel>((_, _) => new KnowledgeSetItemModel
        {
            Name = string.Empty,
            Content = string.Empty
        }, MapKnowledgeSetItemToResponse);
    }

    // Umbraco.Code.MapAll
    private static void MapKnowledgeSetToResponse(IAIKnowledgeSet source, KnowledgeSetResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.Name = source.Name;
        target.Description = source.Description;
        target.Icon = source.Icon;
        target.ItemCount = context.HasItems && context.Items.TryGetValue(ItemCountKey, out var itemCount) && itemCount is int count
            ? count
            : 0;
    }

    // Umbraco.Code.MapAll
    private static void MapKnowledgeSetToDetailResponse(IAIKnowledgeSet source, KnowledgeSetDetailResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.Name = source.Name;
        target.Description = source.Description;
        target.Icon = source.Icon;

        var items = context.HasItems && context.Items.TryGetValue(ItemsKey, out var value) && value is IReadOnlyList<AIKnowledgeSetItem> resolvedItems
            ? resolvedItems
            : [];

        target.Items = context.MapEnumerable<AIKnowledgeSetItem, KnowledgeSetItemModel>(items);
    }

    // Umbraco.Code.MapAll
    private static void MapKnowledgeSetItemToResponse(AIKnowledgeSetItem source, KnowledgeSetItemModel target, MapperContext context)
    {
        target.Name = source.Name;
        target.Description = source.Description;
        target.Content = source.Content;
    }
}
