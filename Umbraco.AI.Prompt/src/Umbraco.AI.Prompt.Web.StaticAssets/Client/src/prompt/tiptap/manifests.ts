import { UAI_TIPTAP_PROMPTS_EXTENSION_ALIAS, UAI_TIPTAP_TOOLBAR_PROMPTS_ALIAS } from './constants.js';

export const promptTiptapManifests: Array<UmbExtensionManifest> = [
    // TipTap extension: appears in the RTE extensions picker in data type config
    {
        type: 'tiptapExtension',
        alias: UAI_TIPTAP_PROMPTS_EXTENSION_ALIAS,
        name: 'AI Prompts Tiptap Extension',
        api: () => import('./prompts.tiptap-api.js'),
        meta: {
            icon: 'icon-wand',
            label: 'AI Prompts',
            group: '#tiptap_extGroup_utilities',
        },
    },
    // TipTap toolbar extension: the toolbar dropdown button
    {
        type: 'tiptapToolbarExtension',
        alias: UAI_TIPTAP_TOOLBAR_PROMPTS_ALIAS,
        name: 'AI Prompts Tiptap Toolbar Extension',
        element: () => import('./prompts-tiptap-toolbar.element.js'),
        forExtensions: [UAI_TIPTAP_PROMPTS_EXTENSION_ALIAS],
        meta: {
            alias: 'ai-prompts',
            icon: 'icon-wand',
            label: 'AI',
        },
    },
];
