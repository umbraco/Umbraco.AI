import React from "react";
import { CopilotKit } from "@copilotkit/react-core";
import { CopilotChat } from "@copilotkit/react-ui";
import "@copilotkit/react-ui/styles.css";

interface CopilotChatPanelProps {
  agentId: string;
  agentName?: string;
}

export const CopilotChatPanel: React.FC<CopilotChatPanelProps> = ({
  agentId,
  agentName = "Agent",
}) => {
  const runtimeUrl = `/umbraco/ai/management/api/v1/agents/${agentId}/stream`;

  return (
    <CopilotKit runtimeUrl={runtimeUrl}>
      <CopilotChat
        className="copilot-chat-container"
        labels={{
          initial: `Hi! I'm ${agentName}. How can I help you?`,
        }}
      />
    </CopilotKit>
  );
};
