namespace Umbraco.AI.Core.Security;

/// <summary>
/// The verbs of the AI rights ticked on the CMS user group screen. Each is stored as a user group
/// permission, so the values must never change.
/// </summary>
public static class AIPermissions
{
    /// <summary>Create, update, delete and test connections.</summary>
    public const string ManageConnections = "Uai.Connection.Manage";

    /// <summary>Create, update and delete profiles.</summary>
    public const string ManageProfiles = "Uai.Profile.Manage";

    /// <summary>Create, update and delete contexts.</summary>
    public const string ManageContexts = "Uai.Context.Manage";

    /// <summary>Create, update and delete knowledge sets.</summary>
    public const string ManageKnowledgeSets = "Uai.KnowledgeSet.Manage";

    /// <summary>Create, update and delete guardrails.</summary>
    public const string ManageGuardrails = "Uai.Guardrail.Manage";

    /// <summary>Change AI settings.</summary>
    public const string ManageSettings = "Uai.Settings.Manage";

    /// <summary>View audit logs.</summary>
    public const string ViewAuditLogs = "Uai.AuditLog.View";

    /// <summary>View usage analytics.</summary>
    public const string ViewAnalytics = "Uai.Analytics.View";

    /// <summary>Create, update, delete and run tests.</summary>
    public const string ManageTests = "Uai.Test.Manage";
}
