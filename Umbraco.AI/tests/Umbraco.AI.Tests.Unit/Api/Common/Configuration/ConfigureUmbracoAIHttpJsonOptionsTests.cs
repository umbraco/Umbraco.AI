using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Umbraco.AI.Web.Api.Common.Configuration;
using HttpJsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;
using MvcJsonOptions = Microsoft.AspNetCore.Mvc.JsonOptions;

namespace Umbraco.AI.Tests.Unit.Api.Common.Configuration;

public class ConfigureUmbracoAIHttpJsonOptionsTests
{
    private const string DocumentName = "ai-management";

    private sealed class DummyConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o) => 0;
        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions o) { }
    }

    private static ConfigureUmbracoAIHttpJsonOptions CreateSut(MvcJsonOptions mvcOptions)
    {
        var monitor = new Mock<IOptionsMonitor<MvcJsonOptions>>();
        monitor.Setup(m => m.Get(DocumentName)).Returns(mvcOptions);
        return new ConfigureUmbracoAIHttpJsonOptions(DocumentName, monitor.Object);
    }

    [Fact]
    public void Configure_MatchingDocument_CopiesEnumConverterAndSetsStrictNumbers()
    {
        // Arrange — MVC options carry both the string-enum converter and an unrelated custom converter.
        var mvc = new MvcJsonOptions();
        mvc.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        mvc.JsonSerializerOptions.Converters.Add(new DummyConverter());
        mvc.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

        var sut = CreateSut(mvc);
        var http = new HttpJsonOptions();

        // Act
        sut.Configure(DocumentName, http);

        // Assert — the enum converter is copied; the unrelated converter is NOT (it would make its
        // target type opaque in schema generation); numbers are strict; naming policy is mirrored.
        http.SerializerOptions.Converters.OfType<JsonStringEnumConverter>().ShouldHaveSingleItem();
        http.SerializerOptions.Converters.OfType<DummyConverter>().ShouldBeEmpty();
        http.SerializerOptions.NumberHandling.ShouldBe(JsonNumberHandling.Strict);
        http.SerializerOptions.PropertyNamingPolicy.ShouldBe(JsonNamingPolicy.CamelCase);
    }

    [Fact]
    public void Configure_DoesNotCopyTypeInfoResolver()
    {
        // The MVC resolver alphabetizes properties for deterministic wire output; copying it would
        // re-order every schema, diverging from the declaration order clients have always used.
        var mvc = new MvcJsonOptions();
        mvc.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        mvc.JsonSerializerOptions.TypeInfoResolver =
            new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver();

        var sut = CreateSut(mvc);
        var http = new HttpJsonOptions();

        sut.Configure(DocumentName, http);

        http.SerializerOptions.TypeInfoResolver.ShouldNotBe(mvc.JsonSerializerOptions.TypeInfoResolver);
    }

    [Fact]
    public void Configure_NonMatchingDocument_IsNoOp()
    {
        var mvc = new MvcJsonOptions();
        mvc.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

        var sut = CreateSut(mvc);
        var http = new HttpJsonOptions();

        sut.Configure("some-other-document", http);

        http.SerializerOptions.Converters.ShouldBeEmpty();
    }
}
