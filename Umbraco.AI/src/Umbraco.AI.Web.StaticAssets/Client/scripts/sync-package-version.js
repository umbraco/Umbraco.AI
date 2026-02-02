import { readFileSync, writeFileSync } from 'fs';

console.log('[Prebuild] Syncing package version');

const packageFile = './package.json';
const packageJson = JSON.parse(readFileSync(packageFile, 'utf8'));

if (process.env.NPM_PACKAGE_VERSION)
{
    console.log('-- resolved version from environment variable --');
    packageJson['version'] = process.env.NPM_PACKAGE_VERSION;
}
else
{
    const versionFile = '../../../version.json';
    const versionJson = JSON.parse(readFileSync(versionFile, 'utf8'));

    console.log('-- resolved version from version file --');
    packageJson['version'] = versionJson['version'];
}

writeFileSync(packageFile, JSON.stringify(packageJson, null, 2), 'utf8');

console.log(`version: ${packageJson['version']}`);
