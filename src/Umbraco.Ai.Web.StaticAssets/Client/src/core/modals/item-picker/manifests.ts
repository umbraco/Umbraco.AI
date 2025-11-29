export const manifests: UmbExtensionManifest[] = [
	{
		type: 'modal',
		alias: 'Uc.Modal.ItemPicker',
		name: 'Item Picker Modal',
		element: () => import('./item-picker-modal.element.js'),
	},
];
