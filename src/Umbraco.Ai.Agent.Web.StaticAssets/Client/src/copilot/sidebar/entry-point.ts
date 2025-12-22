import type { UmbEntryPointOnInit, UmbEntryPointOnUnload } from "@umbraco-cms/backoffice/extension-api";
import { UmbCopilotContext } from "../copilot.context.js";

// The sidebar element - imported so it registers the custom element
import "./copilot-sidebar.element.js";

let sidebarElement: HTMLElement | null = null;

export const onInit: UmbEntryPointOnInit = (host, _extensionRegistry) => {
  // Provide the copilot context globally from the host
  new UmbCopilotContext(host);

  // Create and append the sidebar element to the host's shadow root
  sidebarElement = document.createElement("uai-copilot-sidebar");
  host.shadowRoot?.appendChild(sidebarElement);

  console.log("Copilot sidebar initialized");
};

export const onUnload: UmbEntryPointOnUnload = (_host, _extensionRegistry) => {
  // Remove the sidebar element on unload
  if (sidebarElement) {
    sidebarElement.remove();
    sidebarElement = null;
  }
  console.log("Copilot sidebar unloaded");
};
