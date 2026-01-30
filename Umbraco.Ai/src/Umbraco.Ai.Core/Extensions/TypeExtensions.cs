namespace Umbraco.Ai.Extensions;

internal static class TypeExtensions
{
    public static bool IsNullable(this Type type)
    {
        if (!type.IsValueType) return false;
        return Nullable.GetUnderlyingType(type) != null;
    }
}