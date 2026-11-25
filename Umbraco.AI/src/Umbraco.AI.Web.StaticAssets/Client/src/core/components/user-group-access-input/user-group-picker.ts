import type { UmbLitElement } from "@umbraco-cms/backoffice/lit-element";
import { UMB_MODAL_MANAGER_CONTEXT } from "@umbraco-cms/backoffice/modal";
import { UMB_USER_GROUP_PICKER_MODAL, type UmbUserGroupItemRepository } from "@umbraco-cms/backoffice/user-group";
import { UAI_ITEM_PICKER_MODAL } from "../../modals/item-picker/item-picker-modal.token.js";

export interface UaiUserGroupPickerOptions {
    /** Allow picking more than one group. */
    multiple: boolean;
    /**
     * Limit the choice to these groups. The CMS picker can't be limited, and lists groups through an
     * endpoint only admins may call, so when set a generic item picker lists just these groups.
     * When unset the CMS user group picker opens.
     */
    restrictToIds?: readonly string[];
}

/** Opens the right user group picker and returns the picked group keys, or none if cancelled. */
export async function pickUserGroups(
    host: UmbLitElement,
    repository: UmbUserGroupItemRepository,
    options: UaiUserGroupPickerOptions,
): Promise<string[]> {
    const modalManager = await host.getContext(UMB_MODAL_MANAGER_CONTEXT);
    if (!modalManager) return [];

    const { multiple, restrictToIds } = options;
    if (restrictToIds?.length === 0) return [];
    if (!restrictToIds) {
        const modal = modalManager.open(host, UMB_USER_GROUP_PICKER_MODAL, { data: { multiple } });
        const value = await modal.onSubmit().catch(() => undefined);
        return value?.selection?.filter((id): id is string => !!id) ?? [];
    }

    const modal = modalManager.open(host, UAI_ITEM_PICKER_MODAL, {
        data: {
            fetchItems: async () => {
                if (restrictToIds.length === 0) return [];
                const { data } = await repository.requestItems([...restrictToIds]);
                return (data ?? []).map((group) => ({
                    value: group.unique,
                    label: group.name,
                    icon: group.icon ?? "icon-users",
                }));
            },
            selectionMode: multiple ? "multiple" : "single",
            title: host.localize.term("user_chooseUserGroup", multiple),
        },
    });
    const value = await modal.onSubmit().catch(() => undefined);
    return value?.selection?.map((item) => item.value) ?? [];
}
