import chalk from 'chalk';
import { createClient, defaultPlugins } from '@hey-api/openapi-ts';
import http from 'http';
import { execSync } from 'child_process';

// Parse command line arguments
const args = process.argv.slice(2);
if (args.length < 1) {
  console.error(chalk.red('ERROR: Missing required arguments'));
  console.error('Usage: node generate-openapi.js <swagger-endpoint> [output-dir]');
  console.error('Example: node generate-openapi.js ai-management');
  console.error('Example: node generate-openapi.js ai-management src/custom-output');
  process.exit(1);
}

const swaggerEndpoint = args[0];
const outputDir = args[1] || 'src/api';

// Construct full swagger path
const swaggerPath = `umbraco/swagger/${swaggerEndpoint}/swagger.json`;

// Start notifying user we are generating the TypeScript client
console.log(chalk.green("Generating OpenAPI client..."));

function getUniqueIdentifier() {
  const sanitize = (name) => name.replace(/[^a-zA-Z0-9\-_.]/g, '') || 'default';

  try {
    const gitDir = execSync('git rev-parse --git-dir', { encoding: 'utf-8' }).trim();

    // Check if this is a worktree
    if (gitDir.includes('worktrees')) {
      const parts = gitDir.split(/[\\/]/);
      const worktreeIndex = parts.findIndex(p => p === 'worktrees');
      if (worktreeIndex >= 0 && worktreeIndex + 1 < parts.length) {
        return sanitize(parts[worktreeIndex + 1]);
      }
    }

    // Main worktree - use branch name
    const branch = execSync('git branch --show-current', { encoding: 'utf-8' }).trim();
    return sanitize(branch || 'detached');
  } catch {
    return 'default';
  }
}

// Get named pipe path based on git worktree/branch
const identifier = getUniqueIdentifier();
const pipeName = `umbraco.demosite.${identifier}`;
const socketPath = process.platform === 'win32'
  ? `\\\\.\\pipe\\${pipeName}`
  : `/tmp/${pipeName}`;

console.log(chalk.cyan(`Using named pipe: ${pipeName}`));
console.log(`Fetching ${chalk.yellow(`pipe://${pipeName}/${swaggerPath}`)}`);

// Fetch OpenAPI spec via named pipe
const specData = await new Promise((resolve, reject) => {
  http.get({ socketPath, path: `/${swaggerPath}` }, (res) => {
    let data = '';
    res.setEncoding('utf8');
    res.on('data', (chunk) => data += chunk);
    res.on('end', () => {
      res.statusCode === 200
        ? resolve(data)
        : reject(new Error(`HTTP ${res.statusCode} ${res.statusMessage}`));
    });
  }).on('error', reject);
});

console.log(`OpenAPI spec fetched successfully`);
console.log(`Calling ${chalk.yellow('hey-api')} to generate TypeScript client`);

const spec = JSON.parse(specData);

// Suppress hey-api banner output
const originalLog = console.log;
console.log = () => {};

try {
  await createClient({
    input: spec,
    output: outputDir,
    plugins: [
      ...defaultPlugins,
      '@hey-api/client-fetch',
      {
        name: '@hey-api/sdk',
        asClass: true,
        classNameBuilder: '{{name}}Service',
      }
    ]
  });

  console.log = originalLog;
  console.log(chalk.green('✓ TypeScript client generated successfully'));
} catch (error) {
  console.log = originalLog;
  console.error(`ERROR: Failed to generate client: ${chalk.red(error.message)}`);
  process.exit(1);
}
