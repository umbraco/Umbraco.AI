// Side-effect-only entry point: registers internal components on every page load. Kept
// separate from app.ts, which is also the address other add-ons resolve
// "@umbraco-ai/agent-ui" to -- app.ts gets loaded twice on every page (once as its own
// backofficeEntryPoint, once via the import map whenever another add-on imports from
// "@umbraco-ai/agent-ui"), so anything ONLY reachable through app.ts's exports.js chain
// gets bundled solo into app.js and double-registers ("already been used with this
// registry", confirmed live for uai-chat). Reaching a component from both this file's
// chain and exports.ts's chain makes the bundler split it into its own shared chunk
// instead, which is fetched once regardless of how many entry bundles reference it --
// so every element that exports.ts also value-exports is imported here too, even though
// each is a genuine public export in its own right.
import "./chat/components/hitl-approval.element.js";
import "./chat/components/approval-base.element.js";
import "./chat/components/message-copy-button.element.js";
import "./chat/components/message-regenerate-button.element.js";
import "./chat/components/chat.element.js";
import "./chat/components/message.element.js";
import "./chat/components/input.element.js";
import "./chat/components/agent-status.element.js";
import "./chat/components/tool-renderer.element.js";
import "./chat/components/tool-status.element.js";
import "./chat/components/voice-button.element.js";
