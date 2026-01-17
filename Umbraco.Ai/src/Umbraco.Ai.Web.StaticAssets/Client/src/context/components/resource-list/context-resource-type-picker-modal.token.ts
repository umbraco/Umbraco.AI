import { UmbModalToken } from '@umbraco-cms/backoffice/modal';
import type { UaiContextResourceTypeItemModel } from '../../../context-resource-type/types.js';
import type { UaiResourceOptionsData } from './resource-options-modal.token.js';

export interface UaiContextResourceTypePickerModalData {
    contextResourceTypes: UaiContextResourceTypeItemModel[];
    headline?: string;
}

export interface UaiContextResourceTypePickerModalValue {
    contextResourceType: UaiContextResourceTypeItemModel;
    resource: UaiResourceOptionsData;
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
