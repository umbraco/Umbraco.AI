/**
 * Verbs of the AI rights ticked on the CMS user group screen. Each string is stored as a user
 * group permission, so it must match `Umbraco.AI.Core.Security.AIPermissions` exactly and never change.
 */
export const UAI_PERMISSION_VERB_MANAGE_CONNECTIONS = "Uai.Connection.Manage";
export const UAI_PERMISSION_VERB_MANAGE_PROFILES = "Uai.Profile.Manage";
export const UAI_PERMISSION_VERB_MANAGE_CONTEXTS = "Uai.Context.Manage";
export const UAI_PERMISSION_VERB_MANAGE_KNOWLEDGE_SETS = "Uai.KnowledgeSet.Manage";
export const UAI_PERMISSION_VERB_MANAGE_GUARDRAILS = "Uai.Guardrail.Manage";
export const UAI_PERMISSION_VERB_MANAGE_SETTINGS = "Uai.Settings.Manage";
export const UAI_PERMISSION_VERB_VIEW_AUDIT_LOGS = "Uai.AuditLog.View";
export const UAI_PERMISSION_VERB_VIEW_ANALYTICS = "Uai.Analytics.View";
export const UAI_PERMISSION_VERB_MANAGE_TESTS = "Uai.Test.Manage";

/**
 * The entity type all AI rights are registered for. The user group screen draws one box per entity
 * type, so add-ons register their rights under this same type to share the "AI" box.
 */
export const UAI_PERMISSION_ENTITY_TYPE = "uai-permission";

/**
 * Group names for the `meta.group` of a right. The CMS looks the heading up as
 * `actionCategories_<group>`, so these double as localization keys (defined in lang/en.ts).
 */
export const UAI_PERMISSION_GROUP_CONFIGURATION = "uaiConfiguration";
export const UAI_PERMISSION_GROUP_MONITORING = "uaiMonitoring";
export const UAI_PERMISSION_GROUP_ADD_ONS = "uaiAddOns";
