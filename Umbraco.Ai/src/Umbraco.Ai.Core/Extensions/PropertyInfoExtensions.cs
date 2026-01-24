using System.Reflection;

namespace Umbraco.Ai.Extensions;

internal static class PropertyInfoExtensions
{
    // NullabilityInfoContext is not thread-safe - use ThreadLocal to give each thread its own instance
    private static readonly ThreadLocal<NullabilityInfoContext> NullabilityContext = new(() => new NullabilityInfoContext());

    public static bool IsNullable(this PropertyInfo property)
    {
        // For value types, check if it's Nullable<T>
        if (property.PropertyType.IsValueType)
        {
            return Nullable.GetUnderlyingType(property.PropertyType) != null;
        }

        // For reference types, use NullabilityInfoContext to check NRT annotations
        var nullabilityInfo = NullabilityContext.Value!.Create(property);
        return nullabilityInfo.WriteState == NullabilityState.Nullable;
    }
}
