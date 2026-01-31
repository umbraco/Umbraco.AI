import fetch from 'node-fetch';
import chalk from 'chalk';
import { createClient, defaultPlugins } from '@hey-api/openapi-ts';
import net from 'net';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';
import { execSync } from 'child_process';

const __dirname = dirname(fileURLToPath(import.meta.url));

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
  try {
    const repoRoot = resolve(__dirname, '../');

    // Get git directory path
    const gitDir = execSync('git rev-parse --git-dir', {
      cwd: repoRoot,
      encoding: 'utf-8'
    }).trim();

    const fullGitPath = resolve(repoRoot, gitDir);

    // Check if this is a worktree (contains "worktrees" in path)
    if (fullGitPath.includes('worktrees')) {
      // Extract worktree name from path: .git/worktrees/{name}
      const parts = fullGitPath.split(/[\\/]/);
      const worktreeIndex = parts.findIndex(p => p === 'worktrees');
      if (worktreeIndex >= 0 && worktreeIndex + 1 < parts.length) {
        return sanitizePipeName(parts[worktreeIndex + 1]);
      }
    }

    // Main worktree - use branch name
    const branch = execSync('git branch --show-current', {
      cwd: repoRoot,
      encoding: 'utf-8'
    }).trim();

    return sanitizePipeName(branch || 'detached');
  } catch {
    return 'default';
  }
}

function sanitizePipeName(name) {
  // Remove characters invalid for pipe names (keep alphanumeric, dash, underscore)
  const sanitized = name.replace(/[^a-zA-Z0-9\-_]/g, '');
  return sanitized || 'default';
}

async function connectToPipe(identifier) {
  return new Promise((resolve, reject) => {
    // Platform-specific pipe path with identifier
    const basePath = process.platform === 'win32'
      ? '\\\\.\\pipe\\umbraco-ai-demo-port'
      : '/tmp/umbraco-ai-demo-port';

    const pipePath = `${basePath}-${identifier}`;

    const client = net.connect(pipePath);
    let data = '';

    client.on('connect', () => {
      console.log(chalk.gray('Connected to demo site named pipe'));
    });

    client.on('data', (chunk) => {
      data += chunk.toString();
    });

    client.on('end', () => {
      const port = parseInt(data.trim(), 10);
      if (port && port > 0) {
        resolve(port);
      } else {
        reject(new Error('Invalid port received from pipe'));
      }
    });

    client.on('error', (err) => {
      reject(err);
    });

    // Timeout after 2 seconds
    setTimeout(() => {
      client.destroy();
      reject(new Error('Named pipe connection timeout'));
    }, 2000);
  });
}

async function discoverPort() {
  // Priority 1: Explicit environment variable
  if (process.env.DEMO_PORT) {
    console.log(chalk.cyan(`Using port from DEMO_PORT environment variable: ${process.env.DEMO_PORT}`));
    return process.env.DEMO_PORT;
  }

  // Priority 2: Named pipe (auto-cleaned up, no stale data)
  try {
    const identifier = getUniqueIdentifier();
    console.log(chalk.gray(`Looking for demo site pipe: umbraco-ai-demo-port-${identifier}`));

    const port = await connectToPipe(identifier);
    if (port) {
      console.log(chalk.cyan(`Discovered demo site on port ${port} via named pipe`));
      return port.toString();
    }
  } catch (error) {
    console.log(chalk.yellow(`Named pipe not available: ${error.message}`));
  }

  return null;
}

// Get swagger URL from command line or use port discovery
const discoveredPort = await discoverPort();
let swaggerUrl;

if (discoveredPort) {
  swaggerUrl = `https://127.0.0.1:${discoveredPort}/${swaggerPath}`;
} else {
  // No discovery - use default port
  swaggerUrl = `https://localhost:44355/${swaggerPath}`;
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

  // Suppress hey-api banner output
  const originalLog = console.log;
  console.log = () => {};

  await createClient({
    input: swaggerUrl,
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

  // Restore console.log
  console.log = originalLog;

  console.log(chalk.green('✓ TypeScript client generated successfully'));
  process.exit(0);
})
.catch(error => {
  console.error(`ERROR: Failed to connect to the OpenAPI spec: ${chalk.red(error.message)}`);
  console.error(`The URL to your Umbraco instance may be wrong or the instance is not running`);
  process.exit(1);
});
