using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Umbraco.Abilities.Entities;
using Umbraco.Abilities.Entities.Models;

namespace Umbraco.Abilities.Samples.Cms;

/// <summary>
/// A Rich Text-shaped value: markup plus blocks, where the markup places each block with a tag
/// (<c>&lt;umb-block data-key="…"&gt;&lt;/umb-block&gt;</c>), as the CMS's rich text editor does. Both are ordinary
/// fields, so paths reach them (<c>intro/markup</c>, <c>intro/blocks/{key}/text</c>); the handler adds what a schema
/// can't say.
/// </summary>
public sealed class RichTextValue
{
    public const string BlockGroup = "cms.richText.block";

    [Description("HTML. Blocks are placed with <umb-block data-key=\"…\"></umb-block>; adding or removing a block places or removes its tag.")]
    public string? Markup { get; set; }

    [Items(BlockGroup), Description("The blocks placed in the markup.")]
    public List<RichTextBlock> Blocks { get; set; } = [];
}

public abstract class RichTextBlock : EntityItemModel;

[ItemType("callout", "Callout", Description = "A highlighted note.")]
public sealed class Callout : RichTextBlock
{
    [Required, Description("The note.")]
    public string? Text { get; set; }
}

[ItemType("quote", "Quote", Description = "A quotation.")]
public sealed class Quote : RichTextBlock
{
    [Required, Description("The quotation.")]
    public string? Text { get; set; }

    [Description("Who said it.")]
    public string? Author { get; set; }
}

/// <summary>
/// Keeps the markup's block tags in step with the blocks: a block added is placed before the block now after it, or at
/// the end; one removed takes its tag with it; one moved moves its tag. Every tag must have a block, and every block a tag.
/// </summary>
public sealed partial class RichTextValueHandler : IValueHandler
{
    public string Usage =>
        "Adding a block to 'blocks' places its tag in the markup, before the block at the given position or at the end; " +
        "removing or moving a block removes or moves its tag.";

    public void ItemAdded(JsonObject value, string itemsPath, Guid key) => Place(value, key);

    public void ItemMoved(JsonObject value, string itemsPath, Guid key)
    {
        Unplace(value, key);
        Place(value, key);
    }

    public void ItemRemoved(JsonObject value, string itemsPath, Guid key) => Unplace(value, key);

    public IEnumerable<Problem> Validate(JsonObject value)
    {
        var keys = Blocks(value).Select(b => EntityValues.KeyOf(b).ToString()).ToHashSet();
        var tagged = TagPattern().Matches(Markup(value)).Select(m => m.Groups["key"].Value).ToHashSet();

        foreach (var missing in tagged.Except(keys))
        {
            yield return new Problem("markup", $"The markup places a block {missing} that doesn't exist. Remove its tag or add the block.");
        }

        foreach (var key in keys.Except(tagged))
        {
            yield return new Problem($"blocks/{key}", "This block isn't placed in the markup.");
        }
    }

    // The block's place is a tag in the markup: before the block now after it, else at the end.
    private static void Place(JsonObject value, Guid key)
    {
        var blocks = Blocks(value);
        var index = blocks.FindIndex(b => EntityValues.KeyOf(b) == key);
        var markup = Markup(value);
        var after = index + 1 < blocks.Count ? Tag(EntityValues.KeyOf(blocks[index + 1])) : null;
        value["markup"] = after is not null && markup.Contains(after, StringComparison.Ordinal)
            ? markup.Replace(after, Tag(key) + after, StringComparison.Ordinal)
            : markup + Tag(key);
    }

    private static void Unplace(JsonObject value, Guid key) => value["markup"] = Markup(value).Replace(Tag(key), string.Empty, StringComparison.Ordinal);

    private static List<JsonObject> Blocks(JsonObject value) => (value["blocks"] as JsonArray ?? []).OfType<JsonObject>().ToList();

    private static string Markup(JsonObject value) => value["markup"]?.GetValue<string>() ?? string.Empty;

    private static string Tag(Guid key) => $"<umb-block data-key=\"{key}\"></umb-block>";

    [GeneratedRegex("<umb-block data-key=\"(?<key>[^\"]+)\"></umb-block>")]
    private static partial Regex TagPattern();
}
