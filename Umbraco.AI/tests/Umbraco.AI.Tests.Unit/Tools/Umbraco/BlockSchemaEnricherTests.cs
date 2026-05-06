using System.Text.Json.Nodes;

using Moq;
using Shouldly;

using Umbraco.AI.Core.Tools.Umbraco;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Tests.Unit.Tools.Umbraco;

public class BlockSchemaEnricherTests
{
    [Fact]
    public void Enrich_OnBlockListShape_AttachesAllowedElementTypesAlongsideEnum()
    {
        // Arrange — synthesises the structural shape that BlockListPropertyEditorBase
        // emits in 17.4.0-rc2: a contentTypeKey property with an enum of element-type
        // GUIDs and a values[] array typed as 'any'.
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
                                "values": {
                                    "type": "array",
                                    "items": {
                                        "type": "object",
                                        "properties": {
                                            "alias": { "type": "string" },
                                            "value": {}
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            """)!.AsObject();

        var heroDataType = new PublishedDataType(1, "Umbraco.TextBox", "Umbraco.TextBox", new Lazy<object?>(() => null));
        var ctaDataType = new PublishedDataType(2, "Umbraco.MediaPicker3", "Umbraco.MediaPicker3", new Lazy<object?>(() => null));

        var heroProp = new Mock<IPublishedPropertyType>();
        heroProp.SetupGet(p => p.Alias).Returns("title");
        heroProp.SetupGet(p => p.DataType).Returns(heroDataType);

        var ctaProp = new Mock<IPublishedPropertyType>();
        ctaProp.SetupGet(p => p.Alias).Returns("image");
        ctaProp.SetupGet(p => p.DataType).Returns(ctaDataType);

        var heroElement = new Mock<IPublishedContentType>();
        heroElement.SetupGet(c => c.Key).Returns(heroKey);
        heroElement.SetupGet(c => c.Alias).Returns("heroBlock");
        heroElement.SetupGet(c => c.PropertyTypes).Returns([heroProp.Object]);

        var ctaElement = new Mock<IPublishedContentType>();
        ctaElement.SetupGet(c => c.Key).Returns(ctaKey);
        ctaElement.SetupGet(c => c.Alias).Returns("ctaBlock");
        ctaElement.SetupGet(c => c.PropertyTypes).Returns([ctaProp.Object]);

        var typeCache = new Mock<IPublishedContentTypeCache>();
        typeCache.Setup(t => t.Get(PublishedItemType.Element, heroKey)).Returns(heroElement.Object);
        typeCache.Setup(t => t.Get(PublishedItemType.Element, ctaKey)).Returns(ctaElement.Object);

        var schemaService = new Mock<IPropertyEditorSchemaService>();
        schemaService.Setup(s => s.GetValueSchema("Umbraco.TextBox", It.IsAny<object?>()))
            .Returns(JsonNode.Parse("""{ "type": ["string","null"], "maxLength": 250 }""")!.AsObject());
        schemaService.Setup(s => s.GetValueSchema("Umbraco.MediaPicker3", It.IsAny<object?>()))
            .Returns(JsonNode.Parse("""{ "type": "array", "items": { "type": "object" } }""")!.AsObject());

        // Act
        var enriched = BlockSchemaEnricher.Enrich(blockListSchema, typeCache.Object, schemaService.Object);

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
        var heroProps = hero["properties"]!.AsArray();
        heroProps.Count.ShouldBe(1);
        heroProps[0]!["alias"]!.GetValue<string>().ShouldBe("title");
        heroProps[0]!["editorAlias"]!.GetValue<string>().ShouldBe("Umbraco.TextBox");
        heroProps[0]!["valueSchema"]!["maxLength"]!.GetValue<int>().ShouldBe(250);

        var cta = allowed[1]!.AsObject();
        cta["alias"]!.GetValue<string>().ShouldBe("ctaBlock");
        cta["properties"]!.AsArray()[0]!["editorAlias"]!.GetValue<string>().ShouldBe("Umbraco.MediaPicker3");
    }

    [Fact]
    public void Enrich_StopsAtDepthBoundary()
    {
        // Arrange — a hero element whose only property IS another block list. Default
        // depth budget is 1, so the outer enum should be expanded but the inner
        // (nested) block list's enum should remain bare GUIDs.
        var heroKey = Guid.NewGuid();
        var nestedElementKey = Guid.NewGuid();

        var nestedBlockSchema = JsonNode.Parse($$"""
            {
                "properties": {
                    "contentData": {
                        "items": {
                            "properties": {
                                "contentTypeKey": {
                                    "enum": ["{{nestedElementKey}}"]
                                }
                            }
                        }
                    }
                }
            }
            """)!.AsObject();

        var outerBlockSchema = JsonNode.Parse($$"""
            {
                "properties": {
                    "contentData": {
                        "items": {
                            "properties": {
                                "contentTypeKey": {
                                    "enum": ["{{heroKey}}"]
                                }
                            }
                        }
                    }
                }
            }
            """)!.AsObject();

        var nestedBlockDataType = new PublishedDataType(7, "Umbraco.BlockList", "Umbraco.BlockList", new Lazy<object?>(() => null));
        var heroProp = new Mock<IPublishedPropertyType>();
        heroProp.SetupGet(p => p.Alias).Returns("nested");
        heroProp.SetupGet(p => p.DataType).Returns(nestedBlockDataType);

        var heroElement = new Mock<IPublishedContentType>();
        heroElement.SetupGet(c => c.Key).Returns(heroKey);
        heroElement.SetupGet(c => c.Alias).Returns("heroBlock");
        heroElement.SetupGet(c => c.PropertyTypes).Returns([heroProp.Object]);

        var typeCache = new Mock<IPublishedContentTypeCache>();
        typeCache.Setup(t => t.Get(PublishedItemType.Element, heroKey)).Returns(heroElement.Object);
        typeCache.Setup(t => t.Get(PublishedItemType.Element, nestedElementKey))
            .Throws(new Exception("should not be expanded under a depth=1 budget"));

        var schemaService = new Mock<IPropertyEditorSchemaService>();
        schemaService.Setup(s => s.GetValueSchema("Umbraco.BlockList", It.IsAny<object?>()))
            .Returns(nestedBlockSchema);

        // Act — default depth budget is 1
        var enriched = BlockSchemaEnricher.Enrich(outerBlockSchema, typeCache.Object, schemaService.Object);

        // Assert — outer expanded
        var outerAllowed = enriched!
            ["properties"]!["contentData"]!["items"]!["properties"]!["contentTypeKey"]!
            ["x-allowedElementTypes"]!.AsArray();
        outerAllowed.Count.ShouldBe(1);
        var hero = outerAllowed[0]!.AsObject();
        hero["alias"]!.GetValue<string>().ShouldBe("heroBlock");

        // Assert — inner block list's contentTypeKey enum is preserved but NOT enriched.
        // (The mock would throw if EnrichInPlace tried to resolve the nested key.)
        var innerSchema = hero["properties"]!.AsArray()[0]!["valueSchema"]!.AsObject();
        var innerContentTypeKey = innerSchema
            ["properties"]!["contentData"]!["items"]!["properties"]!["contentTypeKey"]!.AsObject();
        innerContentTypeKey["enum"].ShouldNotBeNull();
        innerContentTypeKey.ContainsKey("x-allowedElementTypes").ShouldBeFalse();
    }

    [Fact]
    public void Enrich_SchemaWithoutContentTypeKey_LeavesItUnchanged()
    {
        var schema = JsonNode.Parse("""
            { "type": ["string","null"], "maxLength": 250 }
            """)!.AsObject();

        var typeCache = new Mock<IPublishedContentTypeCache>();
        var schemaService = new Mock<IPropertyEditorSchemaService>();

        var enriched = BlockSchemaEnricher.Enrich(schema, typeCache.Object, schemaService.Object);

        enriched.ShouldNotBeNull();
        enriched!["maxLength"]!.GetValue<int>().ShouldBe(250);
        typeCache.Verify(t => t.Get(It.IsAny<PublishedItemType>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public void Enrich_NullSchema_ReturnsNull()
    {
        var typeCache = new Mock<IPublishedContentTypeCache>();
        var schemaService = new Mock<IPropertyEditorSchemaService>();

        BlockSchemaEnricher.Enrich(null, typeCache.Object, schemaService.Object).ShouldBeNull();
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
        knownElement.SetupGet(c => c.PropertyTypes).Returns(Array.Empty<IPublishedPropertyType>());

        var typeCache = new Mock<IPublishedContentTypeCache>();
        typeCache.Setup(t => t.Get(PublishedItemType.Element, knownKey)).Returns(knownElement.Object);
        typeCache.Setup(t => t.Get(PublishedItemType.Element, unknownKey))
            .Throws(new Exception("unknown"));
        typeCache.Setup(t => t.Get(PublishedItemType.Content, unknownKey))
            .Throws(new Exception("unknown"));

        var schemaService = new Mock<IPropertyEditorSchemaService>();

        var enriched = BlockSchemaEnricher.Enrich(schema, typeCache.Object, schemaService.Object);

        var allowed = enriched!
            ["properties"]!["contentData"]!["items"]!["properties"]!["contentTypeKey"]!
            ["x-allowedElementTypes"]!.AsArray();
        allowed.Count.ShouldBe(1);
        allowed[0]!["alias"]!.GetValue<string>().ShouldBe("knownBlock");
    }
}
