using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Scoping;

namespace Umbraco.AI.Core.Migrations;

/// <summary>
/// Migration to add the AI section the Admin user group
/// </summary>
public class AddAISectionToAdminGroup : AsyncMigrationBase
{
    private readonly IUserGroupService _userGroupService;
    private readonly IScopeProvider _scopeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddAISectionToAdminGroup"/> class.
    /// </summary>
    public AddAISectionToAdminGroup(IMigrationContext context,
        IScopeProvider scopeProvider,
        IUserGroupService userGroupService)
        : base(context)
    {
        _scopeProvider = scopeProvider;
        _userGroupService = userGroupService;
    }

    /// <summary>
    /// Migrate the database to add the Umbraco AI section to the admin user group
    /// </summary>
    /// <returns></returns>
    protected override async Task MigrateAsync()
    {
        using IScope scope = _scopeProvider.CreateScope();

        // Grant access to this section for the admin group
        IUserGroup? adminUserGroup = await _userGroupService.GetAsync(Cms.Core.Constants.Security.AdminGroupAlias);
        if (adminUserGroup is not null)
        {
            if (adminUserGroup.AllowedSections.Contains(Constants.Sections.AI))
            {
                Logger.LogDebug("The Umbraco AI Application/Section has been assigned to the Admin group already");
            }
            else
            {
                adminUserGroup.AddAllowedSection(Constants.Sections.AI);
                await _userGroupService.UpdateAsync(adminUserGroup, Cms.Core.Constants.Security.SuperUserKey);
            }
        }

        scope.Complete();
    }
}
