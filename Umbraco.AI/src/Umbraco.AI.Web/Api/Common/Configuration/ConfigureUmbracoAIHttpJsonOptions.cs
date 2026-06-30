using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace Umbraco.AI.Web.Api.Common.Configuration;

/// <summary>
/// Bridges the named MVC <see cref="Microsoft.AspNetCore.Mvc.JsonOptions"/> for a single Umbraco AI
/// management document into the matching named HTTP <see cref="JsonOptions"/>, so OpenAPI schema
/// generation uses the same serializer settings (converters, naming policy, type-info resolver) as the
/// wire.
/// </summary>
/// <remarks>
/// Microsoft.AspNetCore.OpenApi generates schemas from the document's named HTTP <see cref="JsonOptions"/>,
/// not the MVC options that drive controller serialization
/// (see <see href="https://github.com/dotnet/aspnetcore/issues/66340"/>). Umbraco CMS works around this for
/// its own document via <c>ConfigureUmbracoBackofficeHttpJsonOptions</c>, but that bridge is hard-scoped to
/// the back-office document, so every Umbraco AI document (Core, Agent, Prompt, …) needs its own.
///
/// Without it, the named HTTP options are empty and schema generation falls back to defaults: enums that
/// rely on the globally-registered <see cref="JsonStringEnumConverter"/> (rather than a type-level
/// <c>[JsonConverter]</c>) are emitted as <c>integer</c> even though the wire value is a string — and the
/// same mismatch affects every other custom converter (e.g. <c>IdOrAlias</c>, the UTC date converters).
/// One instance is registered per document name; <see cref="ReplaceOpenApiSchemaService"/> (wired by the
/// back-office document builder when <c>WithJsonOptions(documentName)</c> is set) then reads these options
/// during schema generation.
/// </remarks>
internal sealed class ConfigureUmbracoAIHttpJsonOptions : IConfigureNamedOptions<JsonOptions>
{
    private readonly string _documentName;
    private readonly IOptionsMonitor<Microsoft.AspNetCore.Mvc.JsonOptions> _mvcJsonOptions;

    public ConfigureUmbracoAIHttpJsonOptions(
        string documentName,
        IOptionsMonitor<Microsoft.AspNetCore.Mvc.JsonOptions> mvcJsonOptions)
    {
        _documentName = documentName;
        _mvcJsonOptions = mvcJsonOptions;
    }

    public void Configure(JsonOptions options) => Configure(Options.DefaultName, options);

    public void Configure(string? name, JsonOptions options)
    {
        // Named options resolve this for every name; only mirror the document this instance owns.
        if (name != _documentName)
        {
            return;
        }

        Microsoft.AspNetCore.Mvc.JsonOptions mvcOptions = _mvcJsonOptions.Get(_documentName);

        // Copy only the string-enum converter. The framework special-cases JsonStringEnumConverter and
        // renders a proper string enum schema (honoring [JsonStringEnumMemberName]); without it in the
        // schema-generation options, enums relying on the globally-registered converter (rather than a
        // type-level [JsonConverter]) are emitted as `integer`.
        //
        // We deliberately do NOT copy our other custom converters. Unlike the enum converter, an
        // arbitrary JsonConverter makes its target type opaque to schema generation — e.g. copying the
        // UTC DateTime converters drops `type: string` from date schemas (clients then see `unknown`),
        // and IdOrAlias/Type are already handled via their own mechanisms. Leaving them out keeps those
        // schemas as the framework's native, correct output.
        foreach (JsonConverter converter in mvcOptions.JsonSerializerOptions.Converters)
        {
            if (converter is JsonStringEnumConverter)
            {
                options.SerializerOptions.Converters.Add(converter);
            }
        }

        options.SerializerOptions.PropertyNamingPolicy = mvcOptions.JsonSerializerOptions.PropertyNamingPolicy;

        // Emit 32/64-bit integers as numeric types instead of the framework default (a string with a
        // numeric pattern), matching v17 and the pre-regression v18 clients.
        options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;

        // NOTE: we deliberately do NOT copy the MVC TypeInfoResolver. It carries an
        // AlphabetizeProperties modifier used for deterministic wire output; applying it to schema
        // generation would re-order every schema's properties alphabetically, diverging from the
        // declaration order the generated clients have always used (v17 and pre-regression v18).
    }
}
