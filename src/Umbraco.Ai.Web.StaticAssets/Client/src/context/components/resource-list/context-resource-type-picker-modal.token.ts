import { UmbModalToken } from '@umbraco-cms/backoffice/modal';
import type { UaiContextResourceTypeItemModel } from '../../../context-resource-type/types.js';

export interface UaiContextResourceTypePickerModalData {
    contextResourceTypes: UaiContextResourceTypeItemModel[];
    headline?: string;
}

export interface UaiContextResourceTypePickerModalValue {
    contextResourceType: UaiContextResourceTypeItemModel;
}

export const UAI_CONTEXT_RESOURCE_TYPE_PICKER_MODAL = new UmbModalToken<
    UaiContextResourceTypePickerModalData,
    UaiContextResourceTypePickerModalValue
>('Uai.Modal.ContextResourceTypePicker', {
    modal: {
        type: 'sidebar',
        size: 'small',
    },
});
