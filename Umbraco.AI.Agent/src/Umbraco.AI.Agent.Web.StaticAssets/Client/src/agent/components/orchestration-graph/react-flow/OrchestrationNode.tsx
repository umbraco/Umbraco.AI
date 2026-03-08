import React, { memo, useCallback } from "react";
import { Handle, Position, type NodeProps } from "@xyflow/react";

import type { UaiNodeConfig } from "../../../types.js";

export interface OrchestrationNodeData extends Record<string, unknown> {
    label: string;
    nodeType: string;
    color: string;
    icon: string;
    config: UaiNodeConfig;
    onEdit?: (nodeId: string) => void;
    onDelete?: (nodeId: string) => void;
}

const nodeStyle: React.CSSProperties = {
    minWidth: 180,
    borderRadius: 8,
    background: "#fff",
    border: "1px solid #e2e8f0",
    boxShadow: "0 1px 3px rgba(0,0,0,0.08)",
    fontSize: 13,
    fontFamily: "var(--uui-font-family, sans-serif)",
    overflow: "hidden",
    cursor: "pointer",
};

const headerStyle = (color: string): React.CSSProperties => ({
    display: "flex",
    alignItems: "center",
    gap: 8,
    padding: "8px 12px",
    background: color,
    color: "#fff",
    fontWeight: 600,
    fontSize: 11,
    textTransform: "uppercase",
    letterSpacing: "0.05em",
});

const bodyStyle: React.CSSProperties = {
    padding: "10px 12px",
    fontSize: 13,
    color: "#1e293b",
    fontWeight: 500,
};

const subtitleStyle: React.CSSProperties = {
    fontSize: 11,
    color: "#64748b",
    fontWeight: 400,
    marginTop: 2,
};

const actionsStyle: React.CSSProperties = {
    display: "flex",
    gap: 4,
    marginLeft: "auto",
};

const actionBtnStyle: React.CSSProperties = {
    background: "none",
    border: "none",
    cursor: "pointer",
    padding: 2,
    borderRadius: 4,
    color: "rgba(255,255,255,0.8)",
    fontSize: 12,
    lineHeight: 1,
};

const handleStyle: React.CSSProperties = {
    width: 10,
    height: 10,
    borderRadius: "50%",
    border: "2px solid #94a3b8",
    background: "#fff",
};

const selectedRing = "0 0 0 2px #3b82f6";

/**
 * Get a contextual subtitle for a node based on its config.
 */
function getSubtitle(nodeType: string, config: UaiNodeConfig): string | null {
    switch (nodeType) {
        case "Agent": {
            const c = config as { agentId?: string | null; isManager?: boolean };
            if (c.isManager) return "Manager";
            return c.agentId ? "Agent selected" : "No agent selected";
        }
        case "ToolCall": {
            const c = config as { toolId?: string | null };
            return c.toolId ?? "No tool selected";
        }
        case "Aggregator": {
            const c = config as { aggregationStrategy?: string | null };
            return c.aggregationStrategy ?? "Concat";
        }
        case "CommunicationBus": {
            const c = config as { maxIterations?: number };
            return `Max ${c.maxIterations ?? 40} iterations`;
        }
        default:
            return null;
    }
}

function OrchestrationNodeComponent({ id, data, selected }: NodeProps) {
    const { label, nodeType, color, icon, config, onEdit, onDelete } = data as unknown as OrchestrationNodeData;
    const isStart = nodeType === "Start";
    const isEnd = nodeType === "End";
    const isStructural = isStart || isEnd;

    const subtitle = !isStructural ? getSubtitle(nodeType, config) : null;

    const handleEdit = useCallback((e: React.MouseEvent) => {
        e.stopPropagation();
        onEdit?.(id);
    }, [id, onEdit]);

    const handleDelete = useCallback((e: React.MouseEvent) => {
        e.stopPropagation();
        onDelete?.(id);
    }, [id, onDelete]);

    return (
        <div
            style={{
                ...nodeStyle,
                borderColor: color,
                boxShadow: selected ? selectedRing : nodeStyle.boxShadow,
            }}
        >
            {!isStart && (
                <Handle
                    type="target"
                    position={Position.Top}
                    style={handleStyle}
                />
            )}

            <div style={headerStyle(color)}>
                <uui-icon name={icon} style={{ fontSize: 14, color: "#fff" } as React.CSSProperties}></uui-icon>
                {nodeType === "CommunicationBus" ? "Bus" : nodeType}
                {!isStructural && (
                    <div style={actionsStyle}>
                        <button
                            style={actionBtnStyle}
                            onClick={handleEdit}
                            title="Edit"
                        >
                            <uui-icon name="icon-edit" style={{ fontSize: 12 } as React.CSSProperties}></uui-icon>
                        </button>
                        <button
                            style={actionBtnStyle}
                            onClick={handleDelete}
                            title="Delete"
                        >
                            <uui-icon name="icon-trash" style={{ fontSize: 12 } as React.CSSProperties}></uui-icon>
                        </button>
                    </div>
                )}
            </div>

            {!isStructural && (
                <div style={bodyStyle}>
                    {label}
                    {subtitle && <div style={subtitleStyle}>{subtitle}</div>}
                </div>
            )}

            {!isEnd && (
                <Handle
                    type="source"
                    position={Position.Bottom}
                    style={handleStyle}
                />
            )}
        </div>
    );
}

export default memo(OrchestrationNodeComponent);

// Declare uui-icon as a valid JSX intrinsic element
declare module "react" {
    // eslint-disable-next-line @typescript-eslint/no-namespace
    namespace JSX {
        interface IntrinsicElements {
            "uui-icon": React.DetailedHTMLProps<
                React.HTMLAttributes<HTMLElement> & { name?: string },
                HTMLElement
            >;
        }
    }
}
