import fetch from 'node-fetch';
import chalk from 'chalk';
import { createClient, defaultPlugins } from '@hey-api/openapi-ts';
import { readFile } from 'fs/promises';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));

// Start notifying user we are generating the TypeScript client
console.log(chalk.green("Generating OpenAPI client..."));

async function discoverPort() {
  // Priority 1: Explicit environment variable
  if (process.env.DEMO_PORT) {
    console.log(chalk.cyan(`Using port from DEMO_PORT environment variable: ${process.env.DEMO_PORT}`));
    return process.env.DEMO_PORT;
  }

  // Priority 2: demo/.env.local file (written by demo site)
  try {
    const repoRoot = resolve(__dirname, '../../../../');
    const envPath = resolve(repoRoot, 'demo/.env.local');
    const envContent = await readFile(envPath, 'utf-8');

    const match = envContent.match(/DEMO_PORT=(\d+)/);
    if (match) {
      console.log(chalk.cyan(`Discovered running demo site on port ${match[1]}`));
      return match[1];
    }
  } catch {
    // File doesn't exist or no port found
  }

  return null;
}

// Get swagger URL from command line or use port discovery
let swaggerUrl = process.argv[2];
const swaggerPath = 'umbraco/swagger/ai-agent-management/swagger.json';

// If no URL provided or it contains the default port, try port discovery
if (!swaggerUrl || swaggerUrl.includes('44355')) {
  const discoveredPort = await discoverPort();
  if (discoveredPort) {
    swaggerUrl = `https://127.0.0.1:${discoveredPort}/${swaggerPath}`;
  } else if (!swaggerUrl) {
    // No discovery and no argument - use default
    swaggerUrl = `https://localhost:44355/${swaggerPath}`;
  }
}

if (!swaggerUrl) {
  console.error(chalk.red(`ERROR: Could not determine OpenAPI spec URL`));
  console.error(`Please ensure the demo site is running or provide a URL as an argument`);
  process.exit(1);
}

// Needed to ignore self-signed certificates from running Umbraco on https on localhost
process.env.NODE_TLS_REJECT_UNAUTHORIZED = '0';

// Start checking to see if we can connect to the OpenAPI spec
console.log("Ensure your Umbraco instance is running");
console.log(`Fetching OpenAPI definition from ${chalk.yellow(swaggerUrl)}`);

fetch(swaggerUrl).then(async (response) => {
  if (!response.ok) {
    console.error(chalk.red(`ERROR: OpenAPI spec returned with a non OK (200) response: ${response.status} ${response.statusText}`));
    console.error(`The URL to your Umbraco instance may be wrong or the instance is not running`);
    process.exit(1);
  }

  console.log(`OpenAPI spec fetched successfully`);
  console.log(`Calling ${chalk.yellow('hey-api')} to generate TypeScript client`);

  await createClient({
    input: swaggerUrl,
    output: 'src/api',
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

  console.log(chalk.green('✓ TypeScript client generated successfully'));
  process.exit(0);
})
.catch(error => {
  console.error(`ERROR: Failed to connect to the OpenAPI spec: ${chalk.red(error.message)}`);
  console.error(`The URL to your Umbraco instance may be wrong or the instance is not running`);
  process.exit(1);
});
