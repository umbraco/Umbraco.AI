import type { ManifestUaiAgentTool } from "../uai-agent-tool.extension.js";

/**
 * Example tool: getCurrentTime
 * A simple frontend tool that returns the current date and time.
 */
const getCurrentTimeManifest: ManifestUaiAgentTool = {
  type: "uaiAgentTool",
  kind: "default",
  alias: "Uai.AgentTool.GetCurrentTime",
  name: "Get Current Time Tool",
  api: () => import("./get-current-time.api.js"),
  meta: {
    toolName: "getCurrentTime",
    label: "Get Current Time",
    description: "Get the current date and time in the user's timezone. Use this when you need to know the current time.",
    icon: "icon-time",
    parameters: {
      type: "object",
      properties: {
        format: {
          type: "string",
          description: "Output format: 'iso' for ISO 8601, 'locale' for localized string, 'unix' for Unix timestamp",
          enum: ["iso", "locale", "unix"],
        },
      },
    },
  },
};

/**
 * Example tool: getPageInfo
 * Returns information about the current Umbraco backoffice page.
 */
const getPageInfoManifest: ManifestUaiAgentTool = {
  type: "uaiAgentTool",
  kind: "default",
  alias: "Uai.AgentTool.GetPageInfo",
  name: "Get Page Info Tool",
  api: () => import("./get-page-info.api.js"),
  meta: {
    toolName: "getPageInfo",
    label: "Get Page Info",
    description: "Get information about the current Umbraco backoffice page including URL, section, and context.",
    icon: "icon-info",
    parameters: {
      type: "object",
      properties: {},
    },
  },
};

/**
 * Example tool: showWeather
 * Demonstrates Generative UI with a custom weather card component.
 */
const showWeatherManifest: ManifestUaiAgentTool = {
  type: "uaiAgentTool",
  kind: "default",
  alias: "Uai.AgentTool.ShowWeather",
  name: "Show Weather Tool",
  api: () => import("./show-weather.api.js"),
  element: () => import("./show-weather.element.js"),
  meta: {
    toolName: "showWeather",
    label: "Weather",
    description: "Get and display the current weather for a location. Shows a visual weather card with temperature, conditions, and details.",
    icon: "icon-cloud",
    parameters: {
      type: "object",
      properties: {
        location: {
          type: "string",
          description: "The city or location to get weather for (e.g., 'London', 'New York', 'Tokyo')",
        },
      },
      required: ["location"],
    },
  },
};

/**
 * Example HITL tool: confirmAction
 * Demonstrates human-in-the-loop approval before executing an action.
 * Uses the default approval element (Approve/Deny buttons).
 */
const confirmActionManifest: ManifestUaiAgentTool = {
  type: "uaiAgentTool",
  kind: "default",
  alias: "Uai.AgentTool.ConfirmAction",
  name: "Confirm Action Tool",
  api: () => import("./confirm-action.api.js"),
  meta: {
    toolName: "confirmAction",
    label: "Confirm Action",
    description:
      "Ask the user to confirm an action before executing it. Use this when you need explicit user approval for an operation.",
    icon: "icon-check",
    // Enable HITL approval with custom config
    approval: {
      // Uses default element (Uai.AgentApprovalElement.Default)
      config: {
        title: "#uAiAgent_approval_defaultTitle",
        // Message will come from LLM args
      },
    },
    parameters: {
      type: "object",
      properties: {
        action: {
          type: "string",
          description: "A description of the action to confirm (e.g., 'delete the About page', 'publish all draft content')",
        },
        message: {
          type: "string",
          description: "Optional detailed message explaining the action and its consequences",
        },
      },
      required: ["action"],
    },
  },
};

/**
 * Example HITL tool: getUserFeedback
 * Demonstrates human-in-the-loop input collection.
 * Uses the input approval element (text field with submit).
 */
const getUserFeedbackManifest: ManifestUaiAgentTool = {
  type: "uaiAgentTool",
  kind: "default",
  alias: "Uai.AgentTool.GetUserFeedback",
  name: "Get User Feedback Tool",
  api: () => import("./get-user-feedback.api.js"),
  meta: {
    toolName: "getUserFeedback",
    label: "Get User Feedback",
    description:
      "Prompt the user for text input or feedback. Use this when you need to collect specific information from the user.",
    icon: "icon-message",
    // Enable HITL with input element
    approval: {
      elementAlias: "Uai.AgentApprovalElement.Input",
      config: {
        // Prompt will come from LLM args
        multiline: false,
      },
    },
    parameters: {
      type: "object",
      properties: {
        topic: {
          type: "string",
          description: "The topic or context for the feedback request",
        },
        prompt: {
          type: "string",
          description: "The question to ask the user",
        },
        placeholder: {
          type: "string",
          description: "Optional placeholder text for the input field",
        },
      },
      required: ["topic", "prompt"],
    },
  },
};

export const manifests = [
  getCurrentTimeManifest,
  getPageInfoManifest,
  showWeatherManifest,
  confirmActionManifest,
  getUserFeedbackManifest,
];
