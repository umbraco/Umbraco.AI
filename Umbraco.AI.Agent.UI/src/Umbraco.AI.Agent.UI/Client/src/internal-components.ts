// Side-effect-only entry point: registers internal components (referenced only by tag
// name in templates, not part of exports.ts) on every page load. Kept separate from
// app.ts, which is also the address other add-ons resolve "@umbraco-ai/agent-ui" to.
import "./chat/components/hitl-approval.element.js";
import "./chat/components/approval-base.element.js";
import "./chat/components/message-copy-button.element.js";
import "./chat/components/message-regenerate-button.element.js";
