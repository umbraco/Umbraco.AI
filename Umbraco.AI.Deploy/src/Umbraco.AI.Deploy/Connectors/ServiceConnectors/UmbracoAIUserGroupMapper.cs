using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Deploy.Connectors.ServiceConnectors;

/// <summary>
/// The one rule for how a user group reference moves between environments, shared by every connector that
/// carries one. User group keys differ per environment, so they travel as aliases.
/// </summary>
/// <remarks>
/// Export: key to alias; a key that matches no group here is kept raw, so the import names it in its error.
/// Import: alias to the target's group; a value that parses as a GUID is looked up by key instead, which
/// keeps artifacts written by older versions (keyed by the source's group key) importable.
/// </remarks>
internal static class UmbracoAIUserGroupMapper
{
    /// <summary>Maps a user group key (as text) to its alias, or returns the value as it is when no group matches.</summary>
    public static async Task<string> GetAliasAsync(IUserGroupService userGroupService, string userGroupKey)
    {
        var userGroup = Guid.TryParse(userGroupKey, out Guid key)
            ? await userGroupService.GetAsync(key).ConfigureAwait(false)
            : null;
        return userGroup?.Alias ?? userGroupKey;
    }

    /// <summary>Maps user group keys to aliases, in order.</summary>
    public static async Task<IReadOnlyList<string>> GetAliasesAsync(
        IUserGroupService userGroupService,
        IEnumerable<Guid> userGroupKeys)
    {
        var aliases = new List<string>();
        foreach (var key in userGroupKeys)
        {
            aliases.Add(await GetAliasAsync(userGroupService, key.ToString()).ConfigureAwait(false));
        }

        return aliases;
    }

    /// <summary>Finds the target's user group by alias, or by key when the value is a GUID.</summary>
    public static async Task<IUserGroup?> FindTargetAsync(IUserGroupService userGroupService, string aliasOrKey)
        => Guid.TryParse(aliasOrKey, out Guid key)
            ? await userGroupService.GetAsync(key).ConfigureAwait(false)
            : await userGroupService.GetAsync(aliasOrKey).ConfigureAwait(false);

    /// <summary>
    /// Maps the allowed group values to the target's keys. A missing group always throws: dropping it
    /// would widen who can use the entity.
    /// </summary>
    /// <param name="userGroupService">The user group service.</param>
    /// <param name="aliasesOrKeys">The values from the artifact.</param>
    /// <param name="entityNoun">The lower-case entity name for the message, such as "agent" or "prompt".</param>
    /// <param name="entityAlias">The alias of the entity being imported.</param>
    public static async Task<IReadOnlyList<Guid>> GetTargetKeysAsync(
        IUserGroupService userGroupService,
        IEnumerable<string> aliasesOrKeys,
        string entityNoun,
        string? entityAlias)
    {
        var keys = new List<Guid>();
        foreach (var value in aliasesOrKeys)
        {
            var userGroup = await FindTargetAsync(userGroupService, value).ConfigureAwait(false)
                ?? throw new InvalidOperationException(
                    $"{char.ToUpperInvariant(entityNoun[0])}{entityNoun[1..]} '{entityAlias}' is limited to user group '{value}', which doesn't exist in this environment. "
                    + "Create a user group with that alias (or, if the value is a key, add the group to the source and export again), "
                    + $"then deploy the {entityNoun} again.");
            keys.Add(userGroup.Key);
        }

        return keys;
    }
}
