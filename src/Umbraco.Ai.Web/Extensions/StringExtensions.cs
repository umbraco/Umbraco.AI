namespace Umbraco.Ai.Web.Extensions;

/// <summary>
/// String extension methods for the Umbraco AI Web project.
/// </summary>
internal static class StringExtensions
{
    /// <summary>
    /// Converts a plural English word to its singular form.
    /// Handles common regular English pluralization patterns.
    /// </summary>
    /// <remarks>
    /// This is the inverse of Umbraco.Extensions.StringExtensions.MakePluralName().
    /// It handles regular plurals but not irregular ones (children, people, etc.).
    /// </remarks>
    public static string MakeSingularName(this string name)
    {
        if (string.IsNullOrEmpty(name) || name.Length < 2)
        {
            return name;
        }

        // Handle -ies -> -y (e.g., "Cities" -> "City", "Capabilities" -> "Capability")
        if (name.EndsWith("ies", StringComparison.OrdinalIgnoreCase) && name.Length > 3)
        {
            return name[..^3] + "y";
        }

        // Handle -xes, -ches, -shes, -sses -> remove "es" (e.g., "Boxes" -> "Box")
        if (name.EndsWith("xes", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("ches", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("shes", StringComparison.OrdinalIgnoreCase) ||
            name.EndsWith("sses", StringComparison.OrdinalIgnoreCase))
        {
            return name[..^2];
        }

        // Handle simple -s -> remove "s" (e.g., "Connections" -> "Connection")
        if (name.EndsWith("s", StringComparison.OrdinalIgnoreCase) &&
            !name.EndsWith("ss", StringComparison.OrdinalIgnoreCase))
        {
            return name[..^1];
        }

        return name;
    }
}
