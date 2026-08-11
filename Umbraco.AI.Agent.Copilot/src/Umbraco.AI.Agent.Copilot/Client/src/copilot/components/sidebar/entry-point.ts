import type { UmbEntryPointOnInit, UmbEntryPointOnUnload } from "@umbraco-cms/backoffice/extension-api";
import { UaiCopilotContext } from "../../copilot.context.js";
import { UaiCopilotFabController } from "../copilot-fab/copilot-fab.controller.js";

let sidebarElement: HTMLElement | null = null;
let fabElement: HTMLElement | null = null;

export const onInit: UmbEntryPointOnInit = (host, _extensionRegistry) => {
    // Provide the copilot context globally from the host
    const copilot = new UaiCopilotContext(host);

    // Create and append the sidebar element to the host's shadow root
    sidebarElement = document.createElement("uai-copilot-sidebar");
    host.shadowRoot?.appendChild(sidebarElement);

    // Mount the single floating action button alongside the sidebar (same stacking context, so the
    // sidebar reliably occludes it while open and reveals it as it animates out). The controller
    // drives its visibility from the copilot's detected entities.
    fabElement = document.createElement("uai-copilot-fab");
    host.shadowRoot?.appendChild(fabElement);
    new UaiCopilotFabController(host, copilot, fabElement);
};

export const onUnload: UmbEntryPointOnUnload = (_host, _extensionRegistry) => {
    // Remove the sidebar and floating button on unload
    if (sidebarElement) {
        sidebarElement.remove();
        sidebarElement = null;
    }
    if (fabElement) {
        fabElement.remove();
        fabElement = null;
    }
};
