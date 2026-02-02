
/**
 * Editable model schema for UI consumption.
 */
export interface UaiEditableModelSchemaModel {
    type?: string;
    fields: UaiEditableModelFieldModel[];
}

/**
 * Editable model field for UI consumption.
 */
export interface UaiEditableModelFieldModel {
    key: string;
    label: string;
    description?: string;
    editorUiAlias?: string;
    editorConfig?: unknown;
    defaultValue?: unknown;
    sortOrder: number;
    isRequired: boolean;
}