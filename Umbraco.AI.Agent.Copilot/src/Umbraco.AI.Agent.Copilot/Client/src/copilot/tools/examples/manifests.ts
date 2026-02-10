import type { ManifestUaiAgentToolRenderer, ManifestUaiAgentFrontendTool } from "@umbraco-ai/agent-ui";

// ─── getCurrentTime ───────────────────────────────────────────────────────────

const getCurrentTimeRendererManifest: ManifestUaiAgentToolRenderer = {
    type: "uaiAgentToolRenderer",
    kind: "default",
    alias: "Uai.AgentToolRenderer.GetCurrentTime",
    name: "Get Current Time Tool Renderer",
    meta: { toolName: "get_current_time", label: "Get Current Time", icon: "icon-time" },
};

const getCurrentTimeFrontendManifest: ManifestUaiAgentFrontendTool = {
    type: "uaiAgentFrontendTool",
    alias: "Uai.AgentFrontendTool.GetCurrentTime",
    name: "Get Current Time Frontend Tool",
    api: () => import("./get-current-time.api.js"),
    meta: {
        toolName: "get_current_time",
        description: "Get the current date and time in the user's timezone.",
        parameters: {
            type: "object",
            properties: {
                format: { type: "string", description: "Output format: 'iso', 'locale', or 'unix'", enum: ["iso", "locale", "unix"] },
            },
        },
    },
};

// ─── getPageInfo ──────────────────────────────────────────────────────────────

const getPageInfoRendererManifest: ManifestUaiAgentToolRenderer = {
    type: "uaiAgentToolRenderer",
    kind: "default",
    alias: "Uai.AgentToolRenderer.GetPageInfo",
    name: "Get Page Info Tool Renderer",
    meta: { toolName: "get_page_info", label: "Get Page Info", icon: "icon-info" },
};

const getPageInfoFrontendManifest: ManifestUaiAgentFrontendTool = {
    type: "uaiAgentFrontendTool",
    alias: "Uai.AgentFrontendTool.GetPageInfo",
    name: "Get Page Info Frontend Tool",
    api: () => import("./get-page-info.api.js"),
    meta: {
        toolName: "get_page_info",
        description: "Get information about the current Umbraco backoffice page including URL, section, and context.",
        parameters: { type: "object", properties: {} },
        scope: "navigation",
    },
};

// ─── showWeather ──────────────────────────────────────────────────────────────

const showWeatherRendererManifest: ManifestUaiAgentToolRenderer = {
    type: "uaiAgentToolRenderer",
    alias: "Uai.AgentToolRenderer.ShowWeather",
    name: "Show Weather Tool Renderer",
    element: () => import("./show-weather.element.js"),
    meta: { toolName: "show_weather", label: "Weather", icon: "icon-cloud" },
};

const showWeatherFrontendManifest: ManifestUaiAgentFrontendTool = {
    type: "uaiAgentFrontendTool",
    alias: "Uai.AgentFrontendTool.ShowWeather",
    name: "Show Weather Frontend Tool",
    api: () => import("./show-weather.api.js"),
    meta: {
        toolName: "show_weather",
        description: "Get and display the current weather for a location.",
        parameters: {
            type: "object",
            properties: {
                location: { type: "string", description: "The city or location to get weather for" },
            },
            required: ["location"],
        },
        scope: "web",
    },
};

// ─── confirmAction (HITL) ─────────────────────────────────────────────────────

const confirmActionRendererManifest: ManifestUaiAgentToolRenderer = {
    type: "uaiAgentToolRenderer",
    kind: "default",
    alias: "Uai.AgentToolRenderer.ConfirmAction",
    name: "Confirm Action Tool Renderer",
    meta: {
        toolName: "confirm_action",
        label: "Confirm Action",
        icon: "icon-check",
        approval: { config: { title: "#uaiChat_approvalDefaultTitle" } },
    },
};

const confirmActionFrontendManifest: ManifestUaiAgentFrontendTool = {
    type: "uaiAgentFrontendTool",
    alias: "Uai.AgentFrontendTool.ConfirmAction",
    name: "Confirm Action Frontend Tool",
    api: () => import("./confirm-action.api.js"),
    meta: {
        toolName: "confirm_action",
        description: "Ask the user to confirm an action before executing it.",
        parameters: {
            type: "object",
            properties: {
                action: { type: "string", description: "A description of the action to confirm" },
                message: { type: "string", description: "Optional detailed message explaining the action" },
            },
            required: ["action"],
        },
    },
};

export const manifests = [
    getCurrentTimeRendererManifest, getCurrentTimeFrontendManifest,
    getPageInfoRendererManifest, getPageInfoFrontendManifest,
    showWeatherRendererManifest, showWeatherFrontendManifest,
    confirmActionRendererManifest, confirmActionFrontendManifest,
];
