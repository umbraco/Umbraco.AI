import { readFileSync, writeFileSync } from 'fs';

if (process.env.BUILD_BUILDID)
{
    console.log('[Prepublish] Cleansing package.json');

    const packageFile = './package.json';
    const packageJson = JSON.parse(readFileSync(packageFile, 'utf8'));

    // Remove all DevDependencies
    delete packageJson.devDependencies;

    // Rename dependencies to peerDependencies
    packageJson.peerDependencies = {
        ...(packageJson.peerDependencies ?? {}),
        ...packageJson.dependencies
    };

    delete packageJson.dependencies;
    delete packageJson.scripts;

    // Write the package.json back to disk
    writeFileSync(packageFile, JSON.stringify(packageJson, null, 2), 'utf8');
}
