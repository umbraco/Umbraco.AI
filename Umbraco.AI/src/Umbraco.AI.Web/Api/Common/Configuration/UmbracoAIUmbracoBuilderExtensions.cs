using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Serialization;
using Umbraco.AI.Web.Api.Common.Configuration;
using Umbraco.AI.Web.Api.Common.Json;
using Umbraco.Cms.Api.Common.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Extension methods for configuring Umbraco AI web services.
/// </summary>
public static class UmbracoAIUmbracoBuilderExtensions
{
    /// <summary>
    /// Adds JSON options for the Umbraco AI application.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="appName"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IUmbracoBuilder AddJsonOptions(this IUmbracoBuilder builder, string appName, Action<JsonSerializerOptions>? configure = null)
    {
        builder.Services.AddControllers()
            .AddJsonOptions(appName, options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.WriteIndented = false;

                options.JsonSerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver
                {
                    Modifiers = { AlphabetizeProperties() },
                };

                options.JsonSerializerOptions.Converters.Add(new IdOrAliasJsonConverter());
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.Converters.Add(new JsonStringTypeConverter());
                options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
                options.JsonSerializerOptions.Converters.Add(new UtcNullableDateTimeJsonConverter());

                configure?.Invoke(options.JsonSerializerOptions);
            });

        // Mirror the relevant MVC JSON options above into the matching named HTTP JsonOptions, which is
        // what Microsoft.AspNetCore.OpenApi uses for schema generation (via the back-office document
        // builder's ReplaceOpenApiSchemaService). Without this, schema generation ignores our global
        // string-enum converter and emits affected enums as `integer`. See ConfigureUmbracoAIHttpJsonOptions.
        builder.Services.AddSingleton<IConfigureOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>(
            sp => new ConfigureUmbracoAIHttpJsonOptions(
                appName,
                sp.GetRequiredService<IOptionsMonitor<Microsoft.AspNetCore.Mvc.JsonOptions>>()));

        return builder;
    }

    private static Action<JsonTypeInfo> AlphabetizeProperties() =>
        static typeInfo =>
        {
            if (typeInfo.Kind != JsonTypeInfoKind.Object)
            {
                return;
            }

            var properties = typeInfo.Properties.OrderBy(p => p.Name, StringComparer.Ordinal).ToList();
            typeInfo.Properties.Clear();
            for (var i = 0; i < properties.Count; i++)
            {
                properties[i].Order = i;
                typeInfo.Properties.Add(properties[i]);
            }
        };
}