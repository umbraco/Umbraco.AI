import type { UmbLocalizationDictionary } from "@umbraco-cms/backoffice/localization-api";

export default {
    uaiGeneral: {
        select: "Select",
        close: "Close",
        inherited: "Inherited",
        allowed: "Allowed",
        denied: "Denied",
        allow: "Allow",
        deny: "Deny",
        toolCount: (count: number) => count === 1 ? "1 tool" : `${count} tools`,
        noResults: "No results found",
    },
    uaiLabels: {
        name: "Name",
        alias: "Alias",
    },
    uaiPlaceholders: {
        enterName: "Enter a name for this item",
        enterAlias: "Enter an alias for this item",
    },
    uaiComponents: {
        pollingButtonTogglePolling: "Toggle Polling",
        pollingButtonPolling: "Polling",
        pollingButtonChoosePollingInterval: "Choose Polling Interval",
        pollingButtonPollingActive: "Polling {0} seconds",
        pollingButtonPollingInterval: "Every {0} seconds",
    },
    uaiCapabilities: {
        chat: "Chat",
        embedding: "Embedding",
        media: "Media",
        moderation: "Moderation",
        speechtotext: "Speech to Text",
        imagegeneration: "Image Generation",
    },
    uaiConnection: {
        deleteConfirm: "Are you sure you want to delete this connection?",
        bulkDeleteConfirm: (count: number) => `Are you sure you want to delete ${count} connection(s)?`,
        actions: "Actions",
        testConnection: "Test Connection",
        testConnectionSuccess: "Connection test successful",
        testConnectionFailed: "Connection test failed",
    },
    uaiProfile: {
        selectProfile: "Select AI profile",
        addProfile: "Add profile",
        noProfilesAvailable: "No AI profiles available. Create one in the AI section.",
        deleteConfirm: "Are you sure you want to delete this profile?",
        bulkDeleteConfirm: (count: number) => `Are you sure you want to delete ${count} profile(s)?`,
    },
    uaiContext: {
        selectContext: "Select AI Context",
        addContext: "Add context",
        noContextsAvailable: "No AI contexts available. Create one in the AI section.",
        deleteConfirm: "Are you sure you want to delete this context?",
        bulkDeleteConfirm: (count: number) => `Are you sure you want to delete ${count} context(s)?`,
    },
    uaiKnowledgeSet: {
        label: "Knowledge Sets",
        description:
            "Background knowledge shipped by installed packages that the AI can draw on. Installed sets are automatically available to every request — there is nothing to configure here.",
        emptyList: "No knowledge sets are currently installed.",
        itemCount: (count: number) => (count === 1 ? "1 item" : `${count} items`),
        surfacedOnDemand:
            "Surfaced to the AI on demand — items are retrieved by the model only when relevant to a request.",
        backToList: "Back to knowledge sets",
        // "Topics" is the user-facing label for a knowledge set's items. The domain/API stay generic
        // ("item"); this presentation vocabulary is defined once here so a future relabel is one edit.
        topicsHeading: "Topics",
        topicsDescription: "Topical information that provides additional context to AI operations.",
        noTopics: "This knowledge set contains no topics.",
        contentLabel: "Content",
        infoHeading: "Info",
        idLabel: "Id",
        descriptionLabel: "Description",
        topicHeadline: "Topic",
        contentError: "Failed to load this item's content.",
        close: "Close",
    },
    uaiTool: {
        selectTool: "Select Tools",
        addTool: "Add tool",
        noToolsAvailable: "No tools available",

        // Tool names and descriptions
        // Context tools (system tools - navigation scope)
        getContextResourceLabel: "Get Context Resource",
        getContextResourceDescription: "Retrieve a specific context resource by ID",
        listContextResourcesLabel: "List Context Resources",
        listContextResourcesDescription: "List available context resources that can be retrieved on demand",

        // Content tools (content-read scope)
        getContentByRouteLabel: "Get Content By Route",
        getContentByRouteDescription: "Resolve a URL path to a published content item",
        getContentTreePathLabel: "Get Content Tree Path",
        getContentTreePathDescription: "Retrieve the ancestor chain and position of a content item in the tree",
        getContentTypeSchemaLabel: "Get Content Type Schema",
        getContentTypeSchemaDescription: "Retrieve the schema of a content type including its property definitions",
        getUmbracoContentLabel: "Get Umbraco Content",
        getUmbracoContentDescription: "Retrieve a published content item by its key including all property values",
        getUmbracoContentChildrenLabel: "Get Umbraco Content Children",
        getUmbracoContentChildrenDescription: "List child content items under a parent with optional filtering and paging",

        // Umbraco media tools (media-read scope)
        getUmbracoMediaLabel: "Get Umbraco Media",
        getUmbracoMediaDescription: "Retrieve a media item from Umbraco by ID",

        // Search tools (search scope)
        searchUmbracoLabel: "Search Umbraco",
        searchUmbracoDescription: "Search Umbraco content and media using Examine",
        semanticSearchLabel: "Semantic Search",
        semanticSearchDescription: "Search for semantically similar content using AI embeddings",

        // Web tools (web scope)
        fetchWebpageLabel: "Fetch Web Page",
        fetchWebpageDescription: "Fetch and extract text content from a web page",

        // Automate tools (automate-read / automate-execute scopes)
        listAutomationsLabel: "List Automations",
        listAutomationsDescription: "List available automations that can be triggered",
        runAutomationLabel: "Run Automation",
        runAutomationDescription: "Trigger an automation to run in the background",
        getAutomationRunLabel: "Get Automation Run",
        getAutomationRunDescription: "Check the status and progress of an automation run",
    },
    uaiToolScopes: {
        selectAll: "Select All",
        selectAllDescription: "Select or deselect all tool permissions at once",
    },
    // Per-scope labels and descriptions for the scope picker.
    // Keys must be `{camelCase(scope.id)}Label` / `{camelCase(scope.id)}Description`; the picker
    // resolves them via `localize.term("uaiToolScope_${camelCase(id)}Label")`. Falls back to the
    // raw scope id when no key is registered.
    uaiToolScope: {
        entitySaveLabel: "Save Entity",
        entitySaveDescription: "Persist staged workspace changes (save without publishing)",
        entityPublishLabel: "Publish Entity",
        entityPublishDescription: "Save and publish content to the public site",
    },
    uaiAuditLog: {
        bulkDeleteConfirm: (count: number) => `Are you sure you want to delete ${count} log entry(ies)?`,
    },
    uaiVersionHistory: {
        history: "History",
        version: "Version",
        date: "Date",
        user: "User",
        compare: "Compare",
        current: "current",
        noVersionsYet: "No versions yet",
        pageInfo: (current: number, total: number) => `Page ${current} of ${total}`,
        compareVersions: (from: number, to: number) => `Compare v${from} to Current (v${to})`,
        rollbackDescription: (version: number) =>
            `Rolling back will create a new version with the content from v${version}. This action cannot be undone.`,
        rollbackTo: (version: number) => `Rollback to v${version}`,
        rollback: "Rollback",
        changes: "Changes",
        noChanges: "No changes detected",
        oldValue: "Old",
        newValue: "New",
    },
    uaiFields: {
        // Text resource fields
        textContentLabel: "Content",
        textContentDescription: "The text content (plain text or markdown)",

        // Content resource fields
        contentContentIdLabel: "Content node",
        contentContentIdDescription:
            "The content node to ground the AI with. Its current values are injected at request time, respecting your read permissions.",

        // Media resource fields
        mediaMediaLabel: "Media item",
        mediaMediaDescription:
            "The media item to ground the AI with. Its details are injected at request time, respecting your read permissions.",

        // Brand Voice resource fields
        brandVoiceToneDescriptionLabel: "Tone",
        brandVoiceToneDescriptionDescription: 'Description of the tone to use (e.g., "Professional but approachable")',
        brandVoiceTargetAudienceLabel: "Target Audience",
        brandVoiceTargetAudienceDescription: 'Description of the target audience (e.g., "B2B tech decision makers")',
        brandVoiceStyleGuidelinesLabel: "Style Guidelines",
        brandVoiceStyleGuidelinesDescription: 'Style guidelines to follow (e.g., "Use active voice, be concise")',
        brandVoiceAvoidPatternsLabel: "Patterns to Avoid",
        brandVoiceAvoidPatternsDescription: 'Patterns and phrases to avoid (e.g., "Jargon, exclamation marks")',

        // Amazon Bedrock fields
        amazonRegionLabel: "AWS Region",
        amazonRegionDescription: 'The AWS region for Bedrock services (e.g., "us-east-1")',
        amazonAccessKeyIdLabel: "Access Key ID",
        amazonAccessKeyIdDescription: "The AWS Access Key ID for authenticating with Bedrock services",
        amazonSecretAccessKeyLabel: "Secret Access Key",
        amazonSecretAccessKeyDescription: "The AWS Secret Access Key for authenticating with Bedrock services",
        amazonEndpointLabel: "Custom Endpoint",
        amazonEndpointDescription: "Custom endpoint URL for Bedrock services (optional)",
    },
    uaiFieldGroups: {
        generalLabel: "General",
        advancedLabel: "Advanced",
        featuresLabel: "Features",
        settingsLabel: "Settings",
        configLabel: "Configuration",
        contextLabel: "Context",
    },
    uaiValidation: {
        required: "This field is required",
        aliasFormat: "Alias can only contain lowercase letters, numbers, and hyphens",
        aliasExists: "An item with this alias already exists",
        minLength: (min: number) => `Must be at least ${min} characters`,
        maxLength: (max: number) => `Must not exceed ${max} characters`,
        rangeUnderflow: (min: number) => `Value must be at least ${min}`,
        rangeOverflow: (max: number) => `Value must not exceed ${max}`,
        providerRequired: "Please select a provider",
        connectionRequired: "Please select a connection",
        modelRequired: "Please select a model",
        temperatureRange: "Temperature must be between 0 and 2",
        maxTokensMin: "Max tokens must be at least 1",
        gradersRequired: "At least one grader is required",
    },
    uaiUserGroupPermissions: {
        headline: "User Group Permissions",
    },
    uaiTest: {
        testConfiguration: "Test Configuration",
        testFeatureType: "Test Feature Type",
        selectTestFeature: "Select test feature",
        testFeatureDescription: "The type of test to run (e.g., prompt completion, agent tool test)",
        targetEntity: "Target Entity",
        targetEntityDescription: "The entity to test (e.g., a specific prompt or agent)",
        selectTargetEntity: "Select Target Entity",
        selectTarget: "Select Target",
        selectTestTypeFirst: "Select a test type first to choose a target entity.",
        noEntitiesAvailable: "No entities available for this feature type",
        noEntitiesAvailableForType: "No entities available for this feature type.",
        ensurePackageInstalled: "Make sure the required package is installed.",
        bulkDeleteConfirm: (count: number) => `Are you sure you want to delete ${count} test(s)?`,
        bulkRunConfirm: (count: number) =>
            `Run ${count} selected test(s)? This will execute against configured AI providers.`,
        runCompleted: "Test run completed successfully.",
        runFailed: "Test run failed.",
        bulkRunDeleteConfirm: (count: number) =>
            `Are you sure you want to delete ${count} test run(s)? This action cannot be undone.`,
    },
    uaiGuardrail: {
        selectGuardrail: "Select Guardrail",
        addGuardrail: "Add guardrail",
        noGuardrailsAvailable: "No guardrails available. Create one in the AI section.",
        deleteConfirm: "Are you sure you want to delete this guardrail?",
        bulkDeleteConfirm: (count: number) => `Are you sure you want to delete ${count} guardrail(s)?`,
        addRule: "Add Rule",
        editRule: "Edit Rule",
        removeRule: "Remove Rule",
        noRulesConfigured: "No rules configured. Add a rule to define guardrail behavior.",
        selectEvaluator: "Select Evaluator",
        noEvaluatorsAvailable: "No evaluators available.",
        ruleName: "Rule Name",
        rulePhase: "Phase",
        ruleAction: "Action",
        phasePreGenerate: "Pre-Generate",
        phasePostGenerate: "Post-Generate",
        actionBlock: "Block",
        actionWarn: "Warn",
        actionRedact: "Redact",
        evaluatorConfig: "Configuration",
    },
} as UmbLocalizationDictionary;
