import React, { memo } from "react";
import { Handle, Position, type NodeProps } from "@xyflow/react";

import type { UaiOrchestrationNodeConfig } from "../../../types.js";

export interface OrchestrationNodeData extends Record<string, unknown> {
    label: string;
    nodeType: string;
    color: string;
    icon: string;
    config: UaiOrchestrationNodeConfig | Record<string, never>;
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

const labelStyle: React.CSSProperties = {
    padding: "10px 12px",
    fontSize: 13,
    color: "#1e293b",
    fontWeight: 500,
};

const handleStyle: React.CSSProperties = {
    width: 10,
    height: 10,
    borderRadius: "50%",
    border: "2px solid #94a3b8",
    background: "#fff",
};

const selectedRing = "0 0 0 2px #3b82f6";

function OrchestrationNodeComponent({ data, selected }: NodeProps) {
    const { label, nodeType, color, icon } = data as unknown as OrchestrationNodeData;
    const isStart = nodeType === "Start";
    const isEnd = nodeType === "End";

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
                {nodeType}
            </div>

            {!isStart && !isEnd && (
                <div style={labelStyle}>{label}</div>
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
