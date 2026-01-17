using System.Reflection;

namespace Umbraco.Ai.Extensions;

internal static class PropertyInfoExtensions
{
    private static readonly NullabilityInfoContext NullabilityContext = new();

    public static bool IsNullable(this PropertyInfo property)
    {
        // For value types, check if it's Nullable<T>
        if (property.PropertyType.IsValueType)
        {
            return Nullable.GetUnderlyingType(property.PropertyType) != null;
        }

        // For reference types, use NullabilityInfoContext to check NRT annotations
        var nullabilityInfo = NullabilityContext.Create(property);
        return nullabilityInfo.WriteState == NullabilityState.Nullable;
    }
}
