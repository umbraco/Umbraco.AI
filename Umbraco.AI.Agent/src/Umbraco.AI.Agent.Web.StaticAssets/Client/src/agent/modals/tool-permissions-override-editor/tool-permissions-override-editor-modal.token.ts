import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import type {
	UaiToolPermissionsOverrideEditorModalData,
	UaiToolPermissionsOverrideEditorModalValue,
} from "./tool-permissions-override-editor-modal.element.js";

export const UAI_TOOL_PERMISSIONS_OVERRIDE_EDITOR_MODAL = new UmbModalToken<
	UaiToolPermissionsOverrideEditorModalData,
	UaiToolPermissionsOverrideEditorModalValue
>("Uai.Modal.ToolPermissionsOverrideEditor", {
	modal: {
		type: "sidebar",
		size: "medium",
	},
});
