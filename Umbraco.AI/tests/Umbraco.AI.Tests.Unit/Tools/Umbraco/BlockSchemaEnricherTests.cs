using System.Text.Json.Nodes;

using Moq;
using Shouldly;

using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

public class BlockSchemaEnricherTests
{
    [Fact]
    public void Enrich_OnBlockListShape_AttachesShallowAllowedElementTypesAndGuidanceNote()
    {
        // Arrange — synthesises the structural shape that BlockListPropertyEditorBase
        // emits in 17.4.0-rc2: a contentTypeKey property with an enum of element-type
        // GUIDs.
        var heroKey = Guid.NewGuid();
        var ctaKey = Guid.NewGuid();

        var blockListSchema = JsonNode.Parse($$"""
            {
                "type": ["object", "null"],
                "properties": {
                    "contentData": {
                        "type": "array",
                        "items": {
                            "type": "object",
                            "required": ["key", "contentTypeKey"],
                            "properties": {
                                "key": { "type": "string", "format": "uuid" },
                                "contentTypeKey": {
                                    "type": "string",
                                    "format": "uuid",
                                    "enum": ["{{heroKey}}", "{{ctaKey}}"]
                                },
                                "values": { "type": "array" }
                            }
                        }
                    }
                }
            }
            """)!.AsObject();

        var heroElement = new Mock<IPublishedContentType>();
        heroElement.SetupGet(c => c.Key).Returns(heroKey);
        heroElement.SetupGet(c => c.Alias).Returns("heroBlock");

        var ctaElement = new Mock<IPublishedContentType>();
        ctaElement.SetupGet(c => c.Key).Returns(ctaKey);
        ctaElement.SetupGet(c => c.Alias).Returns("ctaBlock");

        var typeCache = new Mock<IPublishedContentTypeCache>();
        typeCache.Setup(t => t.Get(PublishedItemType.Element, heroKey)).Returns(heroElement.Object);
        typeCache.Setup(t => t.Get(PublishedItemType.Element, ctaKey)).Returns(ctaElement.Object);

        // Act
        var enriched = BlockSchemaEnricher.Enrich(blockListSchema, typeCache.Object);

        // Assert: the original is untouched (deep clone)
        blockListSchema["properties"]!["contentData"]!["items"]!["properties"]!["contentTypeKey"]!
            .AsObject().ContainsKey("x-allowedElementTypes").ShouldBeFalse();

        // Assert: the clone gained x-allowedElementTypes alongside the enum
        var contentTypeKey = enriched!
            ["properties"]!["contentData"]!["items"]!["properties"]!["contentTypeKey"]!
            .AsObject();
        contentTypeKey["enum"].ShouldNotBeNull();

        var allowed = contentTypeKey["x-allowedElementTypes"]!.AsArray();
        allowed.Count.ShouldBe(2);

        var hero = allowed[0]!.AsObject();
        hero["key"]!.GetValue<string>().ShouldBe(heroKey.ToString());
        hero["alias"]!.GetValue<string>().ShouldBe("heroBlock");
        // Shallow only — no embedded property schemas
        hero.ContainsKey("properties").ShouldBeFalse();
        hero.ContainsKey("valueSchema").ShouldBeFalse();

        var cta = allowed[1]!.AsObject();
        cta["alias"]!.GetValue<string>().ShouldBe("ctaBlock");

        // Assert: a guidance note is attached pointing at get_content_type_schema
        var note = contentTypeKey["x-allowedElementTypesNote"]!.GetValue<string>();
        note.ShouldContain("get_content_type_schema");
    }

    [Fact]
    public void Enrich_DoesNotRecurseIntoPropertyEditors()
    {
        // Arrange — even when an element type's properties are themselves block
        // editors, the enricher must NOT fetch their schemas. We assert this by
        // not setting up IPropertyEditorSchemaService at all (the enricher no
        // longer takes that dependency).
        var heroKey = Guid.NewGuid();

        var schema = JsonNode.Parse($$"""
            {
                "properties": {
                    "contentData": {
                        "items": {
                            "properties": {
                                "contentTypeKey": { "enum": ["{{heroKey}}"] }
                            }
                        }
                    }
                }
            }
            """)!.AsObject();

        var heroElement = new Mock<IPublishedContentType>();
        heroElement.SetupGet(c => c.Key).Returns(heroKey);
        heroElement.SetupGet(c => c.Alias).Returns("heroBlock");

        var typeCache = new Mock<IPublishedContentTypeCache>();
        typeCache.Setup(t => t.Get(PublishedItemType.Element, heroKey)).Returns(heroElement.Object);

        // Act
        var enriched = BlockSchemaEnricher.Enrich(schema, typeCache.Object);

        // Assert: hero entry exists with key + alias, no properties array
        var allowed = enriched!
            ["properties"]!["contentData"]!["items"]!["properties"]!["contentTypeKey"]!
            ["x-allowedElementTypes"]!.AsArray();
        allowed.Count.ShouldBe(1);
        var hero = allowed[0]!.AsObject();
        hero["alias"]!.GetValue<string>().ShouldBe("heroBlock");
        hero.ContainsKey("properties").ShouldBeFalse();

        // PropertyTypes never accessed — no recursion into element-type internals
        heroElement.VerifyGet(c => c.PropertyTypes, Times.Never);
    }

    [Fact]
    public void Enrich_SchemaWithoutContentTypeKey_LeavesItUnchanged()
    {
        var schema = JsonNode.Parse("""
            { "type": ["string","null"], "maxLength": 250 }
            """)!.AsObject();

        var typeCache = new Mock<IPublishedContentTypeCache>();

        var enriched = BlockSchemaEnricher.Enrich(schema, typeCache.Object);

        enriched.ShouldNotBeNull();
        enriched!["maxLength"]!.GetValue<int>().ShouldBe(250);
        typeCache.Verify(t => t.Get(It.IsAny<PublishedItemType>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public void Enrich_NullSchema_ReturnsNull()
    {
        var typeCache = new Mock<IPublishedContentTypeCache>();

        BlockSchemaEnricher.Enrich(null, typeCache.Object).ShouldBeNull();
    }

    [Fact]
    public void Enrich_UnknownElementTypeKey_SkipsThatEntry()
    {
        var knownKey = Guid.NewGuid();
        var unknownKey = Guid.NewGuid();

        var schema = JsonNode.Parse($$"""
            {
                "properties": {
                    "contentData": {
                        "items": {
                            "properties": {
                                "contentTypeKey": {
                                    "enum": ["{{knownKey}}", "{{unknownKey}}"]
                                }
                            }
                        }
                    }
                }
            }
            """)!.AsObject();

        var knownElement = new Mock<IPublishedContentType>();
        knownElement.SetupGet(c => c.Key).Returns(knownKey);
        knownElement.SetupGet(c => c.Alias).Returns("knownBlock");

        var typeCache = new Mock<IPublishedContentTypeCache>();
        typeCache.Setup(t => t.Get(PublishedItemType.Element, knownKey)).Returns(knownElement.Object);
        typeCache.Setup(t => t.Get(PublishedItemType.Element, unknownKey))
            .Throws(new Exception("unknown"));
        typeCache.Setup(t => t.Get(PublishedItemType.Content, unknownKey))
            .Throws(new Exception("unknown"));

        var enriched = BlockSchemaEnricher.Enrich(schema, typeCache.Object);

        var allowed = enriched!
            ["properties"]!["contentData"]!["items"]!["properties"]!["contentTypeKey"]!
            ["x-allowedElementTypes"]!.AsArray();
        allowed.Count.ShouldBe(1);
        allowed[0]!["alias"]!.GetValue<string>().ShouldBe("knownBlock");
    }
}
