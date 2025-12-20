import r2wc from "@r2wc/react-to-web-component";
import { CopilotChatPanel } from "../react/copilot-chat.js";

const CopilotChatElement = r2wc(CopilotChatPanel, {
  shadow: "open",
  props: {
    agentId: "string",
    agentName: "string",
  },
});

customElements.define("uai-copilot-chat", CopilotChatElement);

export { CopilotChatElement };
