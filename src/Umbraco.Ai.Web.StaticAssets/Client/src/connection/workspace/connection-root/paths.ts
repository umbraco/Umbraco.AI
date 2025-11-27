import { UMB_WORKSPACE_PATH_PATTERN } from "@umbraco-cms/backoffice/workspace";
import { UaiConnectionConstants } from "../../constants.js";
import { UMB_SETTINGS_SECTION_PATHNAME } from "@umbraco-cms/backoffice/settings";

export const UAI_CONNECTION_ROOT_WORKSPACE_PATH = UMB_WORKSPACE_PATH_PATTERN.generateAbsolute({
    sectionName: UMB_SETTINGS_SECTION_PATHNAME,
    entityType: UaiConnectionConstants.EntityType.Root,
});