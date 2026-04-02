import { UAI_TIPTAP_DICTATE_EXTENSION_ALIAS, UAI_TIPTAP_TOOLBAR_DICTATE_ALIAS } from './constants.js';

export const dictateTiptapManifests: Array<UmbExtensionManifest> = [
    // TipTap extension: appears in the RTE extensions picker in data type config
    {
        type: 'tiptapExtension',
        alias: UAI_TIPTAP_DICTATE_EXTENSION_ALIAS,
        name: 'AI Dictate Tiptap Extension',
        api: () => import('./dictate.tiptap-api.js'),
        meta: {
            icon: 'icon-mic',
            label: 'AI Dictate',
            group: '#tiptapGroup_ai',
        },
    },
    // TipTap toolbar extension: the dictate toolbar button
    {
        type: 'tiptapToolbarExtension',
        alias: UAI_TIPTAP_TOOLBAR_DICTATE_ALIAS,
        name: 'AI Dictate Tiptap Toolbar Extension',
        element: () => import('./dictate-tiptap-toolbar.element.js'),
        forExtensions: [UAI_TIPTAP_DICTATE_EXTENSION_ALIAS],
        meta: {
            alias: 'ai-dictate',
            icon: 'icon-mic',
            label: 'Dictate',
        },
    },
];
