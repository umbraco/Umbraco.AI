import {
    UAI_GUARDRAIL_ROOT_WORKSPACE_ALIAS,
    UAI_GUARDRAIL_ROOT_ENTITY_TYPE,
    UAI_GUARDRAIL_ICON,
} from "../../constants.js";
import { UAI_GUARDRAIL_COLLECTION_ALIAS } from "../../collection/constants.js";
import { UMB_WORKSPACE_CONDITION_ALIAS } from "@umbraco-cms/backoffice/workspace";

export const manifests: Array<UmbExtensionManifest> = [
    {
        type: "workspace",
        kind: "default",
        alias: UAI_GUARDRAIL_ROOT_WORKSPACE_ALIAS,
        name: "Guardrail Root Workspace",
        meta: {
            entityType: UAI_GUARDRAIL_ROOT_ENTITY_TYPE,
            headline: "Guardrails",
        },
    },
    {
        type: "workspaceView",
        kind: "collection",
        alias: "UmbracoAI.WorkspaceView.GuardrailRoot.Collection",
        name: "Guardrail Root Collection Workspace View",
        meta: {
            label: "Collection",
            pathname: "collection",
            icon: UAI_GUARDRAIL_ICON,
            collectionAlias: UAI_GUARDRAIL_COLLECTION_ALIAS,
        },
        conditions: [
            {
                alias: UMB_WORKSPACE_CONDITION_ALIAS,
                match: UAI_GUARDRAIL_ROOT_WORKSPACE_ALIAS,
            },
        ],
    },
];
