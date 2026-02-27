using Umbraco.Cms.Core.Packaging;

namespace Umbraco.AI.Core.Migrations;

/// <summary>
/// Migration plan for the Umbraco.AI package, defining the steps required to update the database schema and data
/// </summary>
public class UmbracoAIMigrationPlan : PackageMigrationPlan
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UmbracoAIMigrationPlan"/> class with the package name and alias.
    /// </summary>
    public UmbracoAIMigrationPlan()
        : base("Umbraco.AI", "Umbraco.AI", "UmbracoAI")
    { }

    /// <summary>
    /// Gets the initial state of the migration plan, which is used to determine the starting point for migrations.
    /// </summary>
    public override string InitialState => "{uai-init-state}";

    /// <inheritdoc/>
    public override bool IgnoreCurrentState => false;

    /// <summary>
    /// Gets the initial state of the migration plan, which is used to determine the starting point for migrations.
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    protected override void DefinePlan()
    {
        From("{uai-init-state}")
            .To<AddAISectionToAdminGroup>("E2043C98-2791-46CD-8E6E-4C01B985D4C0");
    }
}
