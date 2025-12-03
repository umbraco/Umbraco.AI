/**
 * Model for prompt registration - contains only the data needed for property action registration.
 */
export interface UaiPromptRegistrationModel {
    unique: string;
    alias: string;
    name: string;
    description: string | null;
    content: string;
}

/**
 * Meta data passed to the prompt property action.
 */
export interface UaiPromptPropertyActionMeta {
    icon: string;
    label: string;
    promptUnique: string;
    promptAlias: string;
    promptContent: string;
    promptDescription: string | null;
}

/**
 * Data passed to the prompt preview modal.
 */
export interface UaiPromptPreviewModalData {
    promptName: string;
    promptDescription: string | null;
    promptContent: string;
}

/**
 * Value returned from the prompt preview modal.
 */
export interface UaiPromptPreviewModalValue {
    action: 'insert' | 'copy' | 'cancel';
    content?: string;
}
