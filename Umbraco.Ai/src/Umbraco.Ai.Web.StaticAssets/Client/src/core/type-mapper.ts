import { UaiEditableModelFieldModel, UaiEditableModelSchemaModel } from "./types.ts";
import { EditableModelFieldModel, EditableModelSchemaModel } from "../api";

export const UaiCommonTypeMapper = {
    
    toEditableModelSchemaModel(response: EditableModelSchemaModel): UaiEditableModelSchemaModel {
        return {
            type: response.type ?? undefined,
            fields: response.fields.map(this.toEditableModelFieldModel),
        };
    },

    toEditableModelFieldModel(response: EditableModelFieldModel): UaiEditableModelFieldModel {
        return {
            key: response.key,
            label: response.label,
            description: response.description ?? undefined,
            editorUiAlias: response.editorUiAlias ?? undefined,
            editorConfig: response.editorConfig ?? undefined,
            defaultValue: response.defaultValue ?? undefined,
            sortOrder: response.sortOrder,
            isRequired: response.isRequired,
        };
    },
};
