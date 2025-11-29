import type { TemplateResult } from "@umbraco-cms/backoffice/external/lit";
import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import type { UaiPickableItemModel } from "./types.js"

export interface UaiItemPickerModalData {
    items?: UaiPickableItemModel[];
    fetchItems?: () => Promise<UaiPickableItemModel[]>;
    selectionMode?: 'single' | 'multiple';
    tagTemplate?: (item: UaiPickableItemModel) => TemplateResult;
    title?: string;
    buttonLabel?: string;
    noResultsMessage?: string;
}

export interface UaiItemPickerModalValue {
    value?: UaiPickableItemModel;
}

export const UAI_ITEM_PICKER_MODAL = new UmbModalToken<
    UaiItemPickerModalData,
    UaiPickableItemModel
>('Uai.Modal.ItemPicker', {
    modal: {
        type: 'sidebar',
        size: 'small',
    },
});
