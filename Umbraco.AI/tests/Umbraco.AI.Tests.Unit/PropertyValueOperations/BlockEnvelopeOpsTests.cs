using System.Text.Json.Nodes;
using Umbraco.AI.Core.PropertyValueOperations;

namespace Umbraco.AI.Tests.Unit.PropertyValueOperations;

public class BlockEnvelopeOpsTests
{
    private const string LayoutKey = "Umbraco.BlockList";

    [Fact]
    public void Empty_ProducesCanonicalShape()
    {
        var envelope = BlockEnvelopeOps.Empty(LayoutKey);

        envelope[BlockEnvelopeOps.LayoutPropertyName].ShouldBeOfType<JsonObject>();
        envelope[BlockEnvelopeOps.LayoutPropertyName]![LayoutKey].ShouldBeOfType<JsonArray>();
        envelope[BlockEnvelopeOps.ContentDataPropertyName].ShouldBeOfType<JsonArray>();
        envelope[BlockEnvelopeOps.SettingsDataPropertyName].ShouldBeOfType<JsonArray>();
        envelope[BlockEnvelopeOps.ExposePropertyName].ShouldBeOfType<JsonArray>();
    }

    [Fact]
    public void AddContentDataEntry_AppendsAndAssignsContentKey()
    {
        var envelope = BlockEnvelopeOps.Empty(LayoutKey);
        var contentTypeKey = Guid.NewGuid();

        var contentKey = BlockEnvelopeOps.AddContentDataEntry(
            envelope,
            contentTypeKey,
            new JsonArray { new JsonObject { ["alias"] = "title", ["value"] = "Hi" } });

        var contentData = (JsonArray)envelope[BlockEnvelopeOps.ContentDataPropertyName]!;
        contentData.Count.ShouldBe(1);
        contentData[0]!["key"]!.GetValue<Guid>().ShouldBe(contentKey);
        contentData[0]!["contentTypeKey"]!.GetValue<Guid>().ShouldBe(contentTypeKey);
    }

    [Fact]
    public void AddLayoutEntry_AppendsByDefault()
    {
        var envelope = BlockEnvelopeOps.Empty(LayoutKey);
        var entry1 = new JsonObject { ["contentKey"] = Guid.NewGuid() };
        var entry2 = new JsonObject { ["contentKey"] = Guid.NewGuid() };

        BlockEnvelopeOps.AddLayoutEntry(envelope, LayoutKey, entry1);
        BlockEnvelopeOps.AddLayoutEntry(envelope, LayoutKey, entry2);

        var layoutArray = (JsonArray)envelope[BlockEnvelopeOps.LayoutPropertyName]![LayoutKey]!;
        layoutArray.Count.ShouldBe(2);
        layoutArray[0]!["contentKey"]!.GetValue<Guid>().ShouldBe(entry1["contentKey"]!.GetValue<Guid>());
    }

    [Fact]
    public void AddLayoutEntry_InsertsAtPosition()
    {
        var envelope = BlockEnvelopeOps.Empty(LayoutKey);
        var first = new JsonObject { ["contentKey"] = Guid.NewGuid() };
        var second = new JsonObject { ["contentKey"] = Guid.NewGuid() };
        var inserted = new JsonObject { ["contentKey"] = Guid.NewGuid() };

        BlockEnvelopeOps.AddLayoutEntry(envelope, LayoutKey, first);
        BlockEnvelopeOps.AddLayoutEntry(envelope, LayoutKey, second);
        BlockEnvelopeOps.AddLayoutEntry(envelope, LayoutKey, inserted, position: 1);

        var layoutArray = (JsonArray)envelope[BlockEnvelopeOps.LayoutPropertyName]![LayoutKey]!;
        layoutArray.Count.ShouldBe(3);
        layoutArray[0]!["contentKey"]!.GetValue<Guid>().ShouldBe(first["contentKey"]!.GetValue<Guid>());
        layoutArray[1]!["contentKey"]!.GetValue<Guid>().ShouldBe(inserted["contentKey"]!.GetValue<Guid>());
        layoutArray[2]!["contentKey"]!.GetValue<Guid>().ShouldBe(second["contentKey"]!.GetValue<Guid>());
    }

    [Fact]
    public void RemoveByContentKey_RemovesAllReferences()
    {
        var envelope = BlockEnvelopeOps.Empty(LayoutKey);
        var keep = Guid.NewGuid();
        var remove = Guid.NewGuid();
        var keepSettings = Guid.NewGuid();
        var removeSettings = Guid.NewGuid();

        // Layout entries reference contentKey + settingsKey; both must be cleaned up on removal.
        BlockEnvelopeOps.AddLayoutEntry(envelope, LayoutKey, new JsonObject
        {
            ["contentKey"] = keep,
            ["settingsKey"] = keepSettings,
        });
        BlockEnvelopeOps.AddLayoutEntry(envelope, LayoutKey, new JsonObject
        {
            ["contentKey"] = remove,
            ["settingsKey"] = removeSettings,
        });

        BlockEnvelopeOps.AddContentDataEntry(envelope, Guid.NewGuid(), null, keep);
        BlockEnvelopeOps.AddContentDataEntry(envelope, Guid.NewGuid(), null, remove);
        BlockEnvelopeOps.AddSettingsDataEntry(envelope, Guid.NewGuid(), null, keepSettings);
        BlockEnvelopeOps.AddSettingsDataEntry(envelope, Guid.NewGuid(), null, removeSettings);

        var exposeArray = BlockEnvelopeOps.GetOrCreateArray(envelope, BlockEnvelopeOps.ExposePropertyName);
        exposeArray.Add(new JsonObject { ["contentKey"] = keep, ["culture"] = null, ["segment"] = null });
        exposeArray.Add(new JsonObject { ["contentKey"] = remove, ["culture"] = null, ["segment"] = null });

        BlockEnvelopeOps.RemoveByContentKey(envelope, LayoutKey, remove);

        var layout = (JsonArray)envelope[BlockEnvelopeOps.LayoutPropertyName]![LayoutKey]!;
        var contentData = (JsonArray)envelope[BlockEnvelopeOps.ContentDataPropertyName]!;
        var settingsData = (JsonArray)envelope[BlockEnvelopeOps.SettingsDataPropertyName]!;
        var expose = (JsonArray)envelope[BlockEnvelopeOps.ExposePropertyName]!;

        layout.Count.ShouldBe(1);
        layout[0]!["contentKey"]!.GetValue<Guid>().ShouldBe(keep);

        contentData.Count.ShouldBe(1);
        contentData[0]!["key"]!.GetValue<Guid>().ShouldBe(keep);

        settingsData.Count.ShouldBe(1);
        settingsData[0]!["key"]!.GetValue<Guid>().ShouldBe(keepSettings);

        expose.Count.ShouldBe(1);
        expose[0]!["contentKey"]!.GetValue<Guid>().ShouldBe(keep);
    }

    [Fact]
    public void MoveInLayout_ReordersByContentKey()
    {
        var envelope = BlockEnvelopeOps.Empty(LayoutKey);
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();

        BlockEnvelopeOps.AddLayoutEntry(envelope, LayoutKey, new JsonObject { ["contentKey"] = a });
        BlockEnvelopeOps.AddLayoutEntry(envelope, LayoutKey, new JsonObject { ["contentKey"] = b });
        BlockEnvelopeOps.AddLayoutEntry(envelope, LayoutKey, new JsonObject { ["contentKey"] = c });

        BlockEnvelopeOps.MoveInLayout(envelope, LayoutKey, c, 0);

        var layout = (JsonArray)envelope[BlockEnvelopeOps.LayoutPropertyName]![LayoutKey]!;
        layout[0]!["contentKey"]!.GetValue<Guid>().ShouldBe(c);
        layout[1]!["contentKey"]!.GetValue<Guid>().ShouldBe(a);
        layout[2]!["contentKey"]!.GetValue<Guid>().ShouldBe(b);
    }

    [Fact]
    public void GetPropertyValue_PrefersExactVariantMatch()
    {
        var entry = new JsonObject
        {
            ["values"] = new JsonArray
            {
                new JsonObject { ["alias"] = "title", ["culture"] = "en-US", ["segment"] = null, ["value"] = "English" },
                new JsonObject { ["alias"] = "title", ["culture"] = "da-DK", ["segment"] = null, ["value"] = "Danish" },
            },
        };

        var result = BlockEnvelopeOps.GetPropertyValue(entry, "title", new AIVariantId("da-DK", null));

        result!.GetValue<string>().ShouldBe("Danish");
    }

    [Fact]
    public void GetPropertyValue_FallsBackToInvariantWhenVariantNotPresent()
    {
        var entry = new JsonObject
        {
            ["values"] = new JsonArray
            {
                new JsonObject { ["alias"] = "title", ["culture"] = null, ["segment"] = null, ["value"] = "Invariant" },
            },
        };

        var result = BlockEnvelopeOps.GetPropertyValue(entry, "title", new AIVariantId("en-US", null));

        result!.GetValue<string>().ShouldBe("Invariant");
    }

    [Fact]
    public void SetPropertyValue_UpdatesExistingVariantEntry()
    {
        var entry = new JsonObject
        {
            ["values"] = new JsonArray
            {
                new JsonObject { ["alias"] = "title", ["culture"] = "en-US", ["segment"] = null, ["value"] = "Old" },
            },
        };

        BlockEnvelopeOps.SetPropertyValue(entry, "title", JsonValue.Create("New"), new AIVariantId("en-US", null));

        var values = (JsonArray)entry["values"]!;
        values.Count.ShouldBe(1);
        values[0]!["value"]!.GetValue<string>().ShouldBe("New");
    }

    [Fact]
    public void SetPropertyValue_AppendsNewEntryForNewVariant()
    {
        var entry = new JsonObject
        {
            ["values"] = new JsonArray
            {
                new JsonObject { ["alias"] = "title", ["culture"] = "en-US", ["segment"] = null, ["value"] = "English" },
            },
        };

        BlockEnvelopeOps.SetPropertyValue(entry, "title", JsonValue.Create("Danish"), new AIVariantId("da-DK", null), editorAlias: "Umbraco.TextBox");

        var values = (JsonArray)entry["values"]!;
        values.Count.ShouldBe(2);
        values[1]!["culture"]!.GetValue<string>().ShouldBe("da-DK");
        values[1]!["value"]!.GetValue<string>().ShouldBe("Danish");
        values[1]!["editorAlias"]!.GetValue<string>().ShouldBe("Umbraco.TextBox");
    }
}
