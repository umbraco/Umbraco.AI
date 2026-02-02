import { manifests as modalManifests } from './modals/manifests.js'
import { manifests as versionHistoryManifests } from './version-history/manifests.js'

export const manifests: UmbExtensionManifest[] = [
    ...modalManifests,
    ...versionHistoryManifests,
]
