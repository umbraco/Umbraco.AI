import type { UmbEntryPointOnInit, UmbEntryPointOnUnload } from "@umbraco-cms/backoffice/extension-api";
import { UaiCopilotContext } from "../../copilot.context.js";

let sidebarElement: HTMLElement | null = null;

export const onInit: UmbEntryPointOnInit = (host, _extensionRegistry) => {
    // Provide the copilot context globally from the host
    new UaiCopilotContext(host);

    // Create and append the sidebar element to the host's shadow root
    sidebarElement = document.createElement("uai-copilot-sidebar");
    host.shadowRoot?.appendChild(sidebarElement);
};

export const onUnload: UmbEntryPointOnUnload = (_host, _extensionRegistry) => {
    // Remove the sidebar element on unload
    if (sidebarElement) {
        sidebarElement.remove();
        sidebarElement = null;
    }
};
