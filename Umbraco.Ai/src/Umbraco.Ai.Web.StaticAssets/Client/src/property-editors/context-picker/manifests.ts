import type { ManifestPropertyEditorUi, ManifestPropertyEditorSchema } from '@umbraco-cms/backoffice/property-editor';

const propertyEditorSchema: ManifestPropertyEditorSchema = {
    type: 'propertyEditorSchema',
    name: 'AI Context Picker Schema',
    alias: 'Uai.ContextPicker',
    meta: {
        defaultPropertyEditorUiAlias: 'Uai.PropertyEditorUi.ContextPicker',
    },
};

const propertyEditorUi: ManifestPropertyEditorUi = {
    type: 'propertyEditorUi',
    alias: 'Uai.PropertyEditorUi.ContextPicker',
    name: 'AI Context Picker Property Editor UI',
    element: () => import('./property-editor-ui-context-picker.element.js'),
    meta: {
        label: 'AI Context Picker',
        icon: 'icon-wand',
        group: 'Umbraco AI',
        propertyEditorSchemaAlias: 'Uai.ContextPicker',
        settings: {
            properties: [
                {
                    alias: 'multiple',
                    label: 'Allow Multiple',
                    description: 'Allow selecting multiple AI contexts',
                    propertyEditorUiAlias: 'Umb.PropertyEditorUi.Toggle',
                },
                {
                    alias: 'min',
                    label: 'Minimum Items',
                    description: 'Minimum number of contexts required (optional)',
                    propertyEditorUiAlias: 'Umb.PropertyEditorUi.Integer',
                },
                {
                    alias: 'max',
                    label: 'Maximum Items',
                    description: 'Maximum number of contexts allowed (optional)',
                    propertyEditorUiAlias: 'Umb.PropertyEditorUi.Integer',
                },
            ],
        },
    },
};

export const contextPickerPropertyEditorManifests = [
    propertyEditorSchema,
    propertyEditorUi,
];
