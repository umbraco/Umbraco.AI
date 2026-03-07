import type { GetSchemes } from "rete";
import type { LitArea2D } from "@retejs/lit-plugin";
import type { MinimapExtra } from "rete-minimap-plugin";
import { ClassicPreset } from "rete";

/**
 * Base node class for orchestration graph nodes.
 * Provides the common structure expected by Rete.js.
 */
export class OrchestrationNode extends ClassicPreset.Node {
    width = 220;
    height = 100;

    constructor(
        label: string,
        public nodeType: string,
        public nodeId: string,
    ) {
        super(label);
    }
}

export class OrchestrationConnection<
    S extends OrchestrationNode = OrchestrationNode,
    T extends OrchestrationNode = OrchestrationNode,
> extends ClassicPreset.Connection<S, T> {
    public edgeId?: string;
    public isDefault?: boolean;
    public priority?: number;
}

export type Schemes = GetSchemes<OrchestrationNode, OrchestrationConnection>;
export type AreaExtra = LitArea2D<Schemes> | MinimapExtra;
