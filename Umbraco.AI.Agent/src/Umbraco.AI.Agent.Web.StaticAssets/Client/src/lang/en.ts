import type { UmbLocalizationDictionary } from "@umbraco-cms/backoffice/localization-api";

export default {
    uAIAgent: {
        deleteConfirm: "Are you sure you want to delete this agent?",
        bulkDeleteConfirm: (count: number) => `Are you sure you want to delete ${count} agent(s)?`,
    },
    uaiAgent: {
        selectScope: "Select Scope",
        addScope: "Add Scope",
        noScopesAvailable: "No scopes available. Scopes are registered by add-on packages.",
        scopesDescription: "Categorize this agent for specific purposes (e.g., Copilot chat)",
        noToolScopesAvailable: "No tool scopes available",

        selectSurface: "Select Surface",
        noSurfacesAvailable: "No surfaces available. Surfaces are registered by add-on packages.",

        // Tool permissions
        toolScopeOverrides: "Tool Scope Overrides",
        toolIdOverrides: "Tool Overrides",
        allowedToolScopes: "Allowed Tool Scopes",
        allowedToolIds: "Allowed Tools",
        noDefaultPermissions: "No default permissions configured",
    },
    uaiToolScope: {
        // Content scopes
        contentReadLabel: "Content (Read)",
        contentReadDescription: "Read content operations - get, search, and list content items",
        contentWriteLabel: "Content (Write)",
        contentWriteDescription: "Modify content operations - create, update, publish, and delete content",

        // Media scopes
        mediaReadLabel: "Media (Read)",
        mediaReadDescription: "Read media operations - get, search, and list media items",
        mediaWriteLabel: "Media (Write)",
        mediaWriteDescription: "Modify media operations - upload, update, delete, and move media",

        // Navigation scope
        navigationLabel: "Navigation",
        navigationDescription: "Site structure navigation - tree, breadcrumb, and children operations",

        // Search scope
        searchLabel: "Search",
        searchDescription: "Search operations - fulltext, semantic, and similarity search",

        // Translation scope
        translationLabel: "Translation",
        translationDescription: "Translation operations - translate and detect language",

        // Web scope
        webLabel: "Web",
        webDescription: "External web operations - fetch and scrape web content",

        // Entity scopes
        entityReadLabel: "Entity (Read)",
        entityReadDescription: "Read entity operations - get and serialize entities",
        entityWriteLabel: "Entity (Write)",
        entityWriteDescription: "Modify entity operations - set properties and save entities",
    },
    uaiToolScopeDomain: {
        content: "Content",
        media: "Media",
        general: "General",
        entity: "Entity",
    },
} as UmbLocalizationDictionary;
