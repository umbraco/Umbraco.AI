using Umbraco.Cms.Core;

namespace Umbraco.Abilities.Entities;

/// <summary>
/// Entity identity is a UDI (<c>umb://document/{key}</c>), as in Deploy and the CMS: an entity type is a UDI entity
/// type, and a definition's type is registered with <see cref="UdiParser"/> when the definition is added. Tools still
/// take and return <c>{ entityType, key }</c> with a dashed key, because a UDI writes its key without dashes and the
/// same key in two forms invites copy errors. They also accept a UDI wherever they take a reference.
/// </summary>
public static class EntityUdis
{
    /// <summary>Registers an entity type as a GUID-keyed UDI type, unless the CMS or a package already has.</summary>
    public static void Register(string entityType)
    {
        if (!UdiParser.GetKnownUdiTypes().ContainsKey(entityType))
        {
            UdiParser.RegisterUdiType(entityType, UdiType.GuidUdi);
        }
    }

    public static GuidUdi For(string entityType, Guid key) => new(entityType, key);

    /// <summary>Reads a UDI string such as <c>umb://document/3f2a9c1e5b7d4e8a9c217d4e5f6a8b90</c>.</summary>
    public static bool TryParse(string? value, out GuidUdi udi)
    {
        udi = null!;
        if (value is null || !value.StartsWith("umb://", StringComparison.Ordinal) || !UdiParser.TryParse(value, out var parsed) || parsed is not GuidUdi guidUdi)
        {
            return false;
        }

        udi = guidUdi;
        return true;
    }

    public static GuidUdi ToUdi(this EntityRef entity) => For(entity.EntityType, entity.Key);
}
