using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Umbraco.Abilities.Entities;

namespace Umbraco.Abilities.Samples.Cms;

/// <summary>
/// A Rich Text-shaped value: markup plus blocks, where the markup places each block with a tag
/// (<c>&lt;umb-block data-key="…"&gt;&lt;/umb-block&gt;</c>), as the CMS's rich text editor does. The value's model is
/// the editor's, not the framework's, so its handler owns it: the paths inside it, adding and removing blocks along with
/// their tags, and the rule that every tag has a block and every block a tag.
/// <code>{ "markup": "&lt;p&gt;Hi&lt;/p&gt;&lt;umb-block data-key=\"…\"&gt;&lt;/umb-block&gt;", "blocks": [ { "key": "…", "type": "callout", "fields": { "text": "…" } } ] }</code>
/// </summary>
public sealed partial class RichTextValueHandler : ValueHandlerBase
{
    /// <summary>The block types allowed in the markup, with their fields; a real editor reads these from its configuration.</summary>
    private static readonly Dictionary<string, (string Field, bool Required)[]> BlockTypes = new()
    {
        ["callout"] = [("text", true)],
        ["quote"] = [("text", true), ("author", false)],
    };

    public override string Usage =>
        "Rich text. Set 'markup' (HTML) on this field, or a block's field at 'blocks/{key}/{field}'. Add a block with " +
        "entity.addItem at fieldPath '<this field>/blocks' (types: callout with text; quote with text and author): it is " +
        "placed at the end of the markup, or before the block at the given position. Removing a block removes its tag.";

    public static JsonObject Schema => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["markup"] = new JsonObject { ["type"] = "string", ["description"] = "HTML. Blocks are placed with <umb-block data-key=\"…\"></umb-block>." },
            ["blocks"] = new JsonObject { ["type"] = "array", ["items"] = new JsonObject { ["type"] = "object" } },
        },
    };

    public override JsonNode? SetValue(JsonNode? value, string innerPath, JsonNode? newValue)
    {
        var rich = Read(value);
        var segments = innerPath.Split('/');
        if (segments is ["markup"])
        {
            rich["markup"] = newValue is JsonValue text && text.TryGetValue<string>(out var markup) ? markup : throw Invalid("markup", "Expected HTML as a string.");
            return rich;
        }

        if (segments is ["blocks", var key, var field])
        {
            var block = Block(rich, key, "blocks");
            if (!BlockTypes[block["type"]!.GetValue<string>()].Any(f => f.Field == field))
            {
                throw Invalid(innerPath, $"No field '{field}' on a {block["type"]} block.");
            }

            ((JsonObject)block["fields"]!)[field] = newValue?.DeepClone();
            return rich;
        }

        throw Invalid(innerPath, "Set 'markup', or 'blocks/{key}/{field}'.");
    }

    public override ValueItem AddItem(JsonNode? value, string innerPath, string itemType, JsonObject? fields, Guid key, int? position)
    {
        if (innerPath != "blocks")
        {
            throw Invalid(innerPath, "Blocks are added at 'blocks'.");
        }

        if (!BlockTypes.ContainsKey(itemType))
        {
            throw Invalid(innerPath, $"No block type '{itemType}'. Types: {string.Join(", ", BlockTypes.Keys)}.");
        }

        var rich = Read(value);
        var blocks = (JsonArray)rich["blocks"]!;
        var index = position is { } p ? Math.Clamp(p, 0, blocks.Count) : blocks.Count;
        var before = index < blocks.Count ? blocks[index]!["key"]!.GetValue<string>() : null;
        blocks.Insert(index, new JsonObject { ["key"] = key.ToString(), ["type"] = itemType, ["fields"] = fields?.DeepClone() ?? new JsonObject() });

        // The block's place is a tag in the markup: before the block now after it, else at the end.
        var markup = rich["markup"]!.GetValue<string>();
        var tag = Tag(key.ToString());
        rich["markup"] = before is null ? markup + tag : markup.Replace(Tag(before), tag + Tag(before), StringComparison.Ordinal);
        return new ValueItem(rich, $"blocks/{key}");
    }

    public override JsonNode? RemoveItem(JsonNode? value, string innerPath, Guid key)
    {
        var rich = Read(value);
        var block = Block(rich, key.ToString(), innerPath);
        ((JsonArray)rich["blocks"]!).Remove(block);
        rich["markup"] = rich["markup"]!.GetValue<string>().Replace(Tag(key.ToString()), string.Empty, StringComparison.Ordinal);
        return rich;
    }

    public override IEnumerable<Problem> Validate(JsonNode? value)
    {
        var rich = Read(value);
        var keys = ((JsonArray)rich["blocks"]!).Select(b => b!["key"]!.GetValue<string>()).ToHashSet();
        var tagged = TagPattern().Matches(rich["markup"]!.GetValue<string>()).Select(m => m.Groups["key"].Value).ToHashSet();

        foreach (var missing in tagged.Except(keys))
        {
            yield return new Problem("markup", $"The markup places a block {missing} that doesn't exist. Remove its tag or add the block.");
        }

        foreach (var block in ((JsonArray)rich["blocks"]!).OfType<JsonObject>())
        {
            var key = block["key"]!.GetValue<string>();
            if (!tagged.Contains(key))
            {
                yield return new Problem($"blocks/{key}", "This block isn't placed in the markup.");
            }

            foreach (var (field, _) in BlockTypes.GetValueOrDefault(block["type"]!.GetValue<string>(), []).Where(f => f.Required))
            {
                if (block["fields"]?[field] is null)
                {
                    yield return new Problem($"blocks/{key}/{field}", "Required.");
                }
            }
        }
    }

    private static JsonObject Read(JsonNode? value)
    {
        var rich = value as JsonObject ?? new JsonObject();
        rich["markup"] ??= string.Empty;
        rich["blocks"] ??= new JsonArray();
        return rich;
    }

    private static JsonObject Block(JsonObject rich, string key, string path)
        => ((JsonArray)rich["blocks"]!).OfType<JsonObject>().FirstOrDefault(b => b["key"]!.GetValue<string>() == key)
            ?? throw Invalid($"{path}/{key}", "No block with this key.");

    private static string Tag(string key) => $"<umb-block data-key=\"{key}\"></umb-block>";

    [GeneratedRegex("<umb-block data-key=\"(?<key>[^\"]+)\"></umb-block>")]
    private static partial Regex TagPattern();
}
