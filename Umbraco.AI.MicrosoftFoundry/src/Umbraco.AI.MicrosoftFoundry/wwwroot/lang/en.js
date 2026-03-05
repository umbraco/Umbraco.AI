export default {
    uaiFields: {
        microsoftFoundryEndpointLabel: "Microsoft AI Foundry Endpoint",
        microsoftFoundryEndpointDescription: "Enter your Microsoft AI Foundry endpoint URL (e.g., https://your-resource.services.ai.azure.com/). Note: Models must be deployed in Microsoft AI Foundry before they can be used.",
        microsoftFoundryApiKeyLabel: "API Key",
        microsoftFoundryApiKeyDescription: "Enter your Microsoft AI Foundry API key. Optional when using Entra ID authentication.",
        microsoftFoundryProjectNameLabel: "Project Name",
        microsoftFoundryProjectNameDescription: "The AI Foundry project name. Found in the AI Foundry portal under project settings. Required to list only deployed models.",
        microsoftFoundryTenantIdLabel: "Tenant ID",
        microsoftFoundryTenantIdDescription: "The Azure AD tenant ID. Required for service principal authentication. When set, the provider can list only your deployed models.",
        microsoftFoundryClientIdLabel: "Client ID",
        microsoftFoundryClientIdDescription: "The application (client) ID of the service principal.",
        microsoftFoundryClientSecretLabel: "Client Secret",
        microsoftFoundryClientSecretDescription: "The client secret for the service principal.",
        microsoftFoundryUseResponsesApiLabel: "Use Responses API",
        microsoftFoundryUseResponsesApiDescription: "Use the OpenAI Responses API instead of Chat Completions. Only available in certain Azure regions. See documentation for supported regions.",
    },
    uaiFieldGroups: {
        advancedLabel: "Advanced",
        apiKeyLabel: "API Key Authentication (Deprecated)",
        entraIdLabel: "Entra ID Authentication",
    },
};
