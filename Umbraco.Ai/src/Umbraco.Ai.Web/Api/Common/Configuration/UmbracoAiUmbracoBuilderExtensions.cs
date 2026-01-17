using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Ai.Core.Serialization;
using Umbraco.Ai.Web.Api.Common.Json;
using Umbraco.Cms.Api.Common.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.Ai.Extensions;

/// <summary>
/// Extension methods for configuring Umbraco AI web services.
/// </summary>
public static class UmbracoAiUmbracoBuilderExtensions
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

                configure?.Invoke(options.JsonSerializerOptions);
            });

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