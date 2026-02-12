import { CSSResult } from 'lit';
import type { ManifestApi } from '@umbraco-cms/backoffice/extension-api';
import type { ManifestElement } from '@umbraco-cms/backoffice/extension-api';
import { nothing } from '@umbraco-cms/backoffice/external/lit';
import { Observable } from 'rxjs';
import type { PropertyValues } from '@umbraco-cms/backoffice/external/lit';
import { TemplateResult } from 'lit-html';
import { UaiAgentState } from '@umbraco-ai/agent';
import { UaiChatMessage } from '@umbraco-ai/agent';
import type { UaiFrontendTool } from '@umbraco-ai/agent';
import { UaiInterruptInfo } from '@umbraco-ai/agent';
import { UaiToolCallInfo } from '@umbraco-ai/agent';
import { UaiToolCallStatus } from '@umbraco-ai/agent';
import type { UmbApi } from '@umbraco-cms/backoffice/extension-api';
import type { UmbContextMinimal } from '@umbraco-cms/backoffice/context-api';
import { UmbContextToken } from '@umbraco-cms/backoffice/context-api';
import { UmbControllerBase } from '@umbraco-cms/backoffice/class-api';
import type { UmbControllerHost } from '@umbraco-cms/backoffice/controller-api';
import type { UmbControllerHostElement } from '@umbraco-cms/backoffice/controller-api';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';

/**
 * Manifest for Agent Approval UI elements.
 *
 * Approval elements are reusable UI components that agent tools can reference
 * by alias for human-in-the-loop interactions.
 *
 * Props interface:
 * - `args` - Tool arguments from the LLM (e.g., contentId, contentName)
 * - `config` - Static config from tool manifest (e.g., custom title/message)
 * - `respond` - Callback to return the user's response
 *
 * Priority order for display values: `config` → `args` → localized defaults
 */
export declare interface ManifestUaiAgentApprovalElement extends ManifestElement<UaiAgentApprovalElement> {
    type: "uaiAgentApprovalElement";
    meta: {
        /** Display label for the approval type */
        label: string;
        /** Description of when to use this approval type */
        description?: string;
        /** Icon for the approval type */
        icon?: string;
    };
}

/**
 * Manifest for browser-executable frontend tools.
 *
 * Frontend tools execute in the browser and are available to any chat surface.
 * Tool availability per surface is designed to use Umbraco's conditions framework
 * on the manifest. Conditions are not implemented in the first pass -- all registered
 * tools resolve everywhere.
 *
 * Execution concerns only -- does NOT handle rendering.
 * For custom tool UI, see ManifestUaiAgentToolRenderer.
 *
 * @example
 * ```typescript
 * // Simple frontend tool
 * const tool: ManifestUaiAgentFrontendTool = {
 *     type: "uaiAgentFrontendTool",
 *     alias: "My.AgentFrontendTool.GetTime",
 *     meta: {
 *         toolName: "get_current_time",
 *         description: "Returns the current date and time",
 *         parameters: { type: "object", properties: {} },
 *     },
 *     api: () => import("./get-time.api.js"),
 * };
 *
 * // Entity-scoped tool (conditions-ready)
 * const tool: ManifestUaiAgentFrontendTool = {
 *     type: "uaiAgentFrontendTool",
 *     alias: "My.AgentFrontendTool.SetProperty",
 *     meta: {
 *         toolName: "set_property_value",
 *         description: "Sets a property value on the current entity",
 *         parameters: { type: "object", properties: { alias: { type: "string" }, value: {} } },
 *         scope: "entity-write",
 *         isDestructive: false,
 *     },
 *     api: () => import("./set-property.api.js"),
 *     // Future: conditions gate when the tool resolves
 *     // conditions: [{ alias: "Umb.Condition.Context", context: "UAI_ENTITY_CONTEXT" }]
 * };
 * ```
 */
export declare interface ManifestUaiAgentFrontendTool extends ManifestApi<UaiAgentToolApi> {
    type: "uaiAgentFrontendTool";
    meta: {
        /** Tool name that matches the AG-UI tool call name */
        toolName: string;
        /** Description for LLM (required) */
        description: string;
        /** JSON Schema for tool parameters (required) */
        parameters: Record<string, unknown>;
        /**
         * Tool scope for permission grouping (e.g., 'entity-write', 'navigation').
         * Used to control which agents can access this tool.
         */
        scope?: string;
        /**
         * Whether the tool performs destructive operations.
         * Used for permission filtering.
         */
        isDestructive?: boolean;
    };
}

/**
 * Manifest for rendering tool status/results in any chat surface.
 *
 * This manifest type handles the visual representation of tool calls:
 * - Custom UI elements for tool-specific rendering (Generative UI)
 * - Approval configuration for HITL (Human-in-the-Loop) interactions
 * - Icon and label for default status indicators
 *
 * Rendering concerns only -- does NOT handle tool execution.
 * For browser-executable tools, see ManifestUaiAgentFrontendTool.
 *
 * @example
 * ```typescript
 * // Backend tool with custom results UI
 * const renderer: ManifestUaiAgentToolRenderer = {
 *     type: "uaiAgentToolRenderer",
 *     alias: "My.AgentToolRenderer.Search",
 *     meta: { toolName: "search_content", icon: "icon-search" },
 *     element: () => import("./search-results.element.js"),
 * };
 *
 * // Tool with HITL approval
 * const renderer: ManifestUaiAgentToolRenderer = {
 *     type: "uaiAgentToolRenderer",
 *     alias: "My.AgentToolRenderer.SetProperty",
 *     meta: {
 *         toolName: "set_property_value",
 *         label: "Set Property Value",
 *         icon: "icon-edit",
 *         approval: true,
 *     },
 * };
 * ```
 */
export declare interface ManifestUaiAgentToolRenderer extends ManifestElement<UaiAgentToolElement> {
    type: "uaiAgentToolRenderer";
    kind?: "default";
    meta: {
        /** Tool name that matches the AG-UI tool call name */
        toolName: string;
        /** Display label for the tool */
        label?: string;
        /** Icon to display with the tool */
        icon?: string;
        /**
         * HITL approval configuration.
         * When specified, tool pauses for user approval before execution.
         * - `true` - Use default approval element with localized defaults
         * - `{ elementAlias?, config? }` - Custom approval element and/or config
         */
        approval?: UaiAgentToolApprovalConfig;
    };
}

declare interface PendingApproval {
    interrupt: UaiInterruptInfo;
    targetMessageId?: string;
}

/**
 * Safely parse JSON with fallback value.
 * @param json The JSON string to parse
 * @param fallback The fallback value if parsing fails (defaults to empty object)
 * @returns The parsed value or fallback
 */
export declare function safeParseJson<T = Record<string, unknown>>(json: string | undefined, fallback?: T): T;

/**
 * Context token for consuming the shared chat context.
 * This is the primary context that shared chat UI components should consume.
 */
export declare const UAI_CHAT_CONTEXT: UmbContextToken<UaiChatContextApi, UaiChatContextApi>;

/**
 * Context token for consuming entity context.
 * Frontend tools that operate on entities consume this context.
 */
export declare const UAI_ENTITY_CONTEXT: UmbContextToken<UaiEntityContextApi, UaiEntityContextApi>;

export declare const UAI_HITL_CONTEXT: UmbContextToken<UaiHitlContext, UaiHitlContext>;

/**
 * Base element type for approval render elements.
 */
declare type UaiAgentApprovalElement = UmbControllerHostElement & UaiAgentApprovalElementProps;

/**
 * Props interface for agent approval elements.
 * All approval elements receive these standardized props.
 */
declare interface UaiAgentApprovalElementProps {
    /** Tool arguments from the LLM */
    args: Record<string, unknown>;
    /** Static config from tool manifest (optional overrides/defaults) */
    config: Record<string, unknown>;
    /** Callback to respond - MUST be called to complete the approval */
    respond: (result: unknown) => void;
}

/**
 * Agent item for agent selector in chat surfaces.
 */
export declare interface UaiAgentItem {
    id: string;
    name: string;
    alias: string;
}

export { UaiAgentState }

/**
 * Agent status component.
 * Shows agent thinking/progress state.
 */
export declare class UaiAgentStatusElement extends UmbLitElement {
    #private;
    state?: UaiAgentState;
    render(): TemplateResult<1> | typeof nothing;
    static styles: CSSResult;
}

/**
 * Tool execution API interface.
 * Implement this to create a tool that can be called by AI agents.
 *
 * Extends UmbApi so it can be used with ManifestApi and the extension registry.
 */
export declare interface UaiAgentToolApi extends UmbApi {
    /**
     * Execute the tool with the given arguments.
     * @param args The arguments passed by the AI agent
     * @returns The result to send back to the agent
     */
    execute(args: Record<string, unknown>): Promise<unknown>;
}

/**
 * HITL approval configuration for tools.
 *
 * When `approval` is specified, the tool will pause before execution
 * to show an approval UI and wait for user response.
 *
 * @example
 * ```typescript
 * // Simplest - use default approval with localized defaults
 * approval: true
 *
 * // With custom config
 * approval: {
 *   config: {
 *     title: "Confirm Deletion",
 *     message: "Are you sure you want to delete this content?"
 *   }
 * }
 *
 * // With custom approval element
 * approval: {
 *   elementAlias: "MyProject.AgentApprovalElement.CustomPreview",
 *   config: { showPreview: true }
 * }
 * ```
 */
export declare type UaiAgentToolApprovalConfig = true | {
    /** Alias of approval element (defaults to 'Uai.AgentApprovalElement.Default') */
    elementAlias?: string;
    /** Static config passed to the approval element */
    config?: Record<string, unknown>;
};

/**
 * Base element type for tool render elements.
 */
export declare type UaiAgentToolElement = UmbControllerHostElement & UaiAgentToolElementProps;

/**
 * Props interface for tool render elements.
 * All tool elements receive these standardized props.
 */
export declare interface UaiAgentToolElementProps {
    /** Arguments passed to the tool by the AI agent */
    args: Record<string, unknown>;
    /** Current execution status of the tool */
    status: UaiAgentToolStatus;
    /** Result from tool execution (when completed) */
    result?: unknown;
}

/**
 * Tool status values matching AG-UI events.
 */
export declare type UaiAgentToolStatus = "pending" | "streaming" | "awaiting_approval" | "executing" | "complete" | "error";

/**
 * Default element for displaying tool call status.
 * Shows an icon, tool name, and loading indicator based on status.
 */
export declare class UaiAgentToolStatusElement extends UmbLitElement implements UaiAgentToolElementProps {
    args: Record<string, unknown>;
    status: UaiAgentToolStatus;
    result?: unknown;
    /** Display name for the tool */
    name: string;
    /** Icon to display */
    icon: string;
    render(): TemplateResult<1>;
    static styles: CSSResult;
}

/**
 * Shared chat context interface.
 *
 * Both UaiCopilotContext and the future UaiChatContext implement this interface.
 * Shared chat components consume UAI_CHAT_CONTEXT. Each surface provides its own implementation.
 *
 * Extends UmbContextMinimal so it can be used with UmbContextToken.
 */
export declare interface UaiChatContextApi extends UmbContextMinimal {
    /** Observable list of chat messages in the current conversation. */
    readonly messages$: Observable<UaiChatMessage[]>;
    /** Observable for streaming text content during assistant response. */
    readonly streamingContent$: Observable<string>;
    /** Observable for the current agent execution state. */
    readonly agentState$: Observable<UaiAgentState | undefined>;
    /** Observable indicating whether an agent run is in progress. */
    readonly isRunning$: Observable<boolean>;
    /** Observable for HITL interrupt state. */
    readonly hitlInterrupt$: Observable<UaiInterruptInfo | undefined>;
    /** Observable for pending approval with target message for inline rendering. */
    readonly pendingApproval$: Observable<PendingApproval | undefined>;
    /** Observable list of available agents. */
    readonly agents: Observable<UaiAgentItem[]>;
    /** Observable for the currently selected agent. */
    readonly selectedAgent: Observable<UaiAgentItem | undefined>;
    /** Tool renderer manager for manifest/element lookup. */
    readonly toolRendererManager: UaiToolRendererManager;
    /** Send a user message to the agent. */
    sendUserMessage(content: string): Promise<void>;
    /** Abort the current agent run. */
    abortRun(): void;
    /** Regenerate the last assistant message. */
    regenerateLastMessage(): void;
    /** Select an agent by ID. */
    selectAgent(agentId: string | undefined): void;
    /** Respond to a HITL interrupt. */
    respondToHitl(response: string): void;
}

/**
 * Main chat component.
 * Renders observables from the shared chat context and forwards user input.
 * Consumes UAI_CHAT_CONTEXT -- works in any surface (copilot, chat, etc.).
 */
export declare class UaiChatElement extends UmbLitElement {
    #private;
    private _agentName;
    private _messages;
    private _agentState?;
    private _pendingApproval?;
    private _isRunning;
    constructor();
    render(): TemplateResult<1>;
    static styles: CSSResult;
}

/**
 * Chat input component.
 * Provides a text input with send button, agent selector, and keyboard support.
 * Consumes UAI_CHAT_CONTEXT for agent data.
 *
 * @fires send - Dispatched when user sends a message
 */
export declare class UaiChatInputElement extends UmbLitElement {
    #private;
    disabled: boolean;
    placeholder: string;
    private _value;
    private _agents;
    private _selectedAgentId;
    constructor();
    updated(changedProperties: PropertyValues): void;
    render(): TemplateResult<1>;
    static styles: CSSResult;
}

export { UaiChatMessage }

/**
 * Chat message component.
 * Renders a single message with markdown support and embedded tool status.
 */
export declare class UaiChatMessageElement extends UmbLitElement {
    #private;
    message: UaiChatMessage;
    isLastAssistantMessage: boolean;
    isRunning: boolean;
    render(): TemplateResult<1>;
    static styles: CSSResult;
}

/**
 * Default fallback handler that clears agent state.
 * Used when no specific handler matches the interrupt reason.
 */
export declare class UaiDefaultInterruptHandler implements UaiInterruptHandler {
    readonly reason = "*";
    handle(_interrupt: UaiInterruptInfo, context: UaiInterruptContext): void;
}

/**
 * Shared entity context contract.
 *
 * Allows tools to operate on an entity without knowing which surface they're in.
 * The copilot provides it by wrapping the host workspace's entity state.
 * The chat could provide it via a side-drawer editor.
 *
 * Tools that operate on entities consume this context -- they don't know
 * which surface they're in. If the context doesn't exist in the current
 * surface, the tool doesn't have access to entity state.
 *
 * Today this is handled by the tool simply not being registered outside copilot.
 * In the future, Umbraco's conditions framework on the manifest would gate tool
 * resolution automatically (e.g., conditions: [{ alias: "Umb.Condition.Context",
 * context: "UAI_ENTITY_CONTEXT" }]).
 *
 * Extends UmbContextMinimal so it can be used with UmbContextToken.
 */
export declare interface UaiEntityContextApi extends UmbContextMinimal {
    /** Observable of the current entity type (e.g., "document", "media"). */
    readonly entityType$: Observable<string | undefined>;
    /** Observable of the current entity key. */
    readonly entityKey$: Observable<string | undefined>;
    /**
     * Get a value from the current entity using a JSON path.
     * @param path JSON path to the value (e.g., "title", "price.amount", "inventory.quantity")
     * @returns The value at the path, or undefined if not found
     */
    getValue(path: string): unknown;
    /**
     * Set a value in the current entity using a JSON path.
     * Changes are staged -- the user must click Save to persist.
     * @param path JSON path to the value (e.g., "title", "price.amount", "inventory.quantity")
     * @param value The value to set
     */
    setValue(path: string, value: unknown): void;
    /** Observable indicating whether the entity has unsaved changes. */
    readonly isDirty$: Observable<boolean>;
}

/**
 * Executes frontend tools and publishes results.
 *
 * Responsibilities:
 * - Executing tools sequentially
 * - Handling HITL approval via UaiHitlContext
 * - Publishing status updates and results via observables
 *
 * Surface-agnostic -- works wherever frontend tools are provided.
 */
export declare class UaiFrontendToolExecutor {
    #private;
    readonly results$: Observable<UaiFrontendToolResult>;
    readonly statusUpdates$: Observable<UaiFrontendToolStatusUpdate>;
    constructor(toolRendererManager: UaiToolRendererManager, frontendToolManager: UaiFrontendToolManager, hitlContext?: UaiHitlContext);
    /**
     * Set the HITL context for approval handling.
     */
    setHitlContext(hitlContext: UaiHitlContext): void;
    /**
     * Execute a list of tool calls sequentially.
     */
    execute(toolCalls: UaiToolCallInfo[]): Promise<void>;
}

/**
 * Frontend tool manager -- handles execution concerns.
 *
 * Observes `uaiAgentFrontendTool` extensions from the extension registry,
 * produces `UaiFrontendTool[]` for the AG-UI client, and provides API loading.
 *
 * Because the manager observes only *resolved* manifests from the extension registry,
 * any future conditions on the manifest are automatically respected. A tool with
 * conditions: [{ alias: "Umb.Condition.Context", context: "UAI_ENTITY_CONTEXT" }]
 * would only appear in frontendTools$ when entity context is provided by the current
 * surface. No filtering logic needed in the manager itself.
 */
export declare class UaiFrontendToolManager extends UmbControllerBase {
    #private;
    /**
     * Observable stream of frontend-executable tools.
     * Emits when tools are added, removed, or updated in the registry.
     */
    readonly frontendTools$: Observable<UaiFrontendTool[]>;
    /**
     * Get the current snapshot of frontend-executable tools with metadata.
     * @returns Array of UaiFrontendTool definitions with scope and permission metadata
     */
    get frontendTools(): UaiFrontendTool[];
    constructor(host: UmbControllerHost);
    /**
     * Check if a tool is a frontend-executable tool.
     * @param toolName The name of the tool
     */
    isFrontendTool(toolName: string): boolean;
    /**
     * Get or load the API instance for a frontend tool.
     * @param toolName The name of the tool
     * @returns The tool API instance
     * @throws Error if tool not found or API fails to load
     */
    getApi(toolName: string): Promise<UaiAgentToolApi>;
}

/**
 * Result of a frontend tool execution.
 */
declare interface UaiFrontendToolResult {
    /** The ID of the tool call this result belongs to */
    toolCallId: string;
    /** The result returned by the tool */
    result: unknown;
    /** Error message if the tool execution failed */
    error?: string;
}

/**
 * Status update for a tool call.
 */
declare interface UaiFrontendToolStatusUpdate {
    /** The ID of the tool call */
    toolCallId: string;
    /** The new status */
    status: UaiToolCallStatus;
}

export declare class UaiHitlContext extends UmbControllerBase {
    #private;
    readonly interrupt$: Observable<UaiInterruptInfo | undefined>;
    readonly targetMessageId$: Observable<string | undefined>;
    readonly pendingApproval$: Observable<    {
    interrupt: UaiInterruptInfo;
    targetMessageId: string | undefined;
    } | undefined>;
    constructor(host: UmbControllerHost);
    setInterrupt(interrupt: UaiInterruptInfo, context: UaiInterruptContext): void;
    respond(response: string): void;
}

/**
 * Handles server-side HITL (human_approval) interrupts.
 * Delegates to UaiHitlContext to show the interrupt UI.
 */
export declare class UaiHitlInterruptHandler extends UmbControllerBase implements UaiInterruptHandler {
    #private;
    readonly reason = "human_approval";
    constructor(host: UmbControllerHost);
    handle(interrupt: UaiInterruptInfo, context: UaiInterruptContext): void;
}

export declare interface UaiInterruptContext {
    resume(response?: unknown): void;
    setAgentState(state?: UaiAgentState): void;
    readonly lastAssistantMessageId?: string;
    readonly messages: readonly UaiChatMessage[];
}

export declare interface UaiInterruptHandler {
    readonly reason: string;
    handle(interrupt: UaiInterruptInfo, context: UaiInterruptContext): void;
}

/**
 * Registry for interrupt handlers.
 * Matches interrupts to handlers by reason, with fallback support.
 */
export declare class UaiInterruptHandlerRegistry {
    #private;
    registerAll(handlers: UaiInterruptHandler[]): void;
    register(handler: UaiInterruptHandler): void;
    handle(interrupt: UaiInterruptInfo, context: UaiInterruptContext): boolean;
    clear(): void;
}

export { UaiInterruptInfo }

/**
 * Shared run controller for managing AG-UI client lifecycle, chat state, and streaming.
 *
 * Refactored from UaiCopilotRunController to accept tool configuration as optional injection.
 * - Copilot creates with frontendToolManager set + UaiToolExecutionHandler
 * - Chat initially creates without frontendToolManager (server-side tools only)
 */
export declare class UaiRunController extends UmbControllerBase {
    #private;
    readonly messages$: Observable<UaiChatMessage[]>;
    readonly streamingContent$: Observable<string>;
    readonly agentState$: Observable<UaiAgentState | undefined>;
    readonly isRunning$: Observable<boolean>;
    /** Expose tool renderer manager for context provision */
    get toolRendererManager(): UaiToolRendererManager;
    constructor(host: UmbControllerHost, hitlContext: UaiHitlContext, config: UaiRunControllerConfig);
    destroy(): void;
    setAgent(agent: UaiAgentItem): void;
    sendUserMessage(content: string, context?: Array<{
        description: string;
        value: string;
    }>): void;
    resetConversation(): void;
    abortRun(): void;
    regenerateLastMessage(): void;
}

/**
 * Configuration for the run controller.
 * Surfaces inject their tool infrastructure and interrupt handlers.
 */
export declare interface UaiRunControllerConfig {
    /** Tool renderer manager for manifest/element lookup */
    toolRendererManager: UaiToolRendererManager;
    /** Optional frontend tool manager -- surfaces that support frontend tools inject this */
    frontendToolManager?: UaiFrontendToolManager;
    /** Interrupt handlers to register */
    interruptHandlers: UaiInterruptHandler[];
}

export { UaiToolCallInfo }

/** Element constructor type for tool UI components */
declare type UaiToolElementConstructor = new () => UaiAgentToolElement;

/**
 * Handles tool_execution interrupts by executing frontend tools.
 *
 * When the server interrupts with reason "tool_execution":
 * 1. Finds frontend tool calls in the last assistant message
 * 2. Executes them via UaiFrontendToolExecutor
 * 3. Resumes the run when all tools complete
 */
export declare class UaiToolExecutionHandler implements UaiInterruptHandler {
    #private;
    private frontendToolManager;
    private executor;
    readonly reason = "tool_execution";
    constructor(frontendToolManager: UaiFrontendToolManager, executor: UaiFrontendToolExecutor);
    handle(_interrupt: UaiInterruptInfo, context: UaiInterruptContext): void;
}

/**
 * Tool renderer component that dynamically renders tool UI based on registered extensions.
 *
 * This is a purely presentational component that:
 * 1. Looks up `uaiAgentToolRenderer` extension by `meta.toolName`
 * 2. If found with `element`, instantiates the custom element
 * 3. Otherwise, renders the default tool status indicator
 *
 * Consumes UAI_CHAT_CONTEXT for the tool renderer manager.
 */
export declare class UaiToolRendererElement extends UmbLitElement {
    #private;
    toolCall: UaiToolCallInfo;
    private _status;
    private _result?;
    private _hasCustomElement;
    connectedCallback(): void;
    updated(changedProperties: Map<string, unknown>): void;
    get manifest(): ManifestUaiAgentToolRenderer | null;
    render(): TemplateResult<1>;
}

/**
 * Tool renderer manager -- handles rendering concerns.
 *
 * Responsibilities:
 * - Observing `uaiAgentToolRenderer` manifests from the extension registry
 * - Providing manifest lookup by tool name (for approval config, icon, label)
 * - Loading and caching tool UI elements (Generative UI)
 *
 * This manager handles ONLY rendering. For execution, see UaiFrontendToolManager.
 */
export declare class UaiToolRendererManager extends UmbControllerBase {
    #private;
    constructor(host: UmbControllerHost);
    /**
     * Get the renderer manifest for a tool by name.
     * @param toolName The name of the tool
     * @returns The tool renderer manifest, or undefined if not found
     */
    getManifest(toolName: string): ManifestUaiAgentToolRenderer | undefined;
    /**
     * Get or load the element constructor for a tool's UI.
     * @param toolName The name of the tool
     * @returns The element constructor, or undefined if tool has no custom UI
     */
    getElement(toolName: string): Promise<UaiToolElementConstructor | undefined>;
}

export { }
