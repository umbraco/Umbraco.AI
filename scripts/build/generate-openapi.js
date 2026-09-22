import chalk from "chalk";
import { createClient } from "@hey-api/openapi-ts";
import https from "https";
import { getPort } from "worktree-dev-port";
import { readFileSync, writeFileSync } from "fs";
import { join } from "path";

// Parse command line arguments
const args = process.argv.slice(2);
if (args.length < 1) {
    console.error(chalk.red("ERROR: Missing required arguments"));
    console.error("Usage: node generate-openapi.js <openapi-document-name> [output-dir]");
    console.error("Example: node generate-openapi.js ai-management");
    console.error("Example: node generate-openapi.js ai-management src/custom-output");
    process.exit(1);
}

const documentName = args[0];
const outputDir = args[1] || "src/api";

// Construct full OpenAPI document path. CMS v18 swapped Swashbuckle for
// Microsoft.AspNetCore.OpenApi, moving the document endpoint to /umbraco/openapi/{name}.json.
const openApiPath = `umbraco/openapi/${documentName}.json`;

// Start notifying user we are generating the TypeScript client
console.log(chalk.green("Generating OpenAPI client..."));

// Get the dev port already assigned to this worktree by the demo site (via
// Umbraco.Community.WorktreeDevPort). The site must already be running.
let port;
try {
    port = getPort();
} catch (error) {
    console.error(chalk.red(`ERROR: ${error.message}`));
    process.exit(1);
}

console.log(chalk.cyan(`Using port ${port} for this worktree`));
console.log(`Fetching ${chalk.yellow(`https://127.0.0.1:${port}/${openApiPath}`)}`);

// Fetch OpenAPI spec over HTTPS using the ASP.NET Core dev cert (self-signed, so skip verification).
// The Host header is pinned to "localhost" (rather than the real 127.0.0.1:<port>) so the server's
// generated OpenAPI doc — and the client baseUrl hey-api derives from it — doesn't bake in this
// worktree's own ephemeral port.
const specData = await new Promise((resolve, reject) => {
    https
        .get(
            {
                hostname: "127.0.0.1",
                port,
                path: `/${openApiPath}`,
                rejectUnauthorized: false,
                headers: { Host: "localhost" },
            },
            (res) => {
                let data = "";
                res.setEncoding("utf8");
                res.on("data", (chunk) => (data += chunk));
                res.on("end", () => {
                    res.statusCode === 200 ? resolve(data) : reject(new Error(`HTTP ${res.statusCode} ${res.statusMessage}`));
                });
            }
        )
        .on("error", reject);
});

console.log(`OpenAPI spec fetched successfully`);
console.log(`Calling ${chalk.yellow("hey-api")} to generate TypeScript client`);

const spec = JSON.parse(specData);

// Suppress hey-api banner output
const originalLog = console.log;
console.log = () => {};

try {
    await createClient({
        input: spec,
        output: outputDir,
        plugins: [
            "@hey-api/typescript",
            "@hey-api/client-fetch",
            {
                name: "@hey-api/sdk",
                operations: {
                    strategy: "byTags",
                    containerName: "{{name}}Service",
                },
            },
        ],
    });

    console.log = originalLog;

    // Post-process generated files to fix AGUI casing
    // hey-api transforms AGUI -> Agui for PascalCase consistency
    // We need to preserve the all-caps AGUI naming from the OpenAPI spec
    const filesToFix = ["types.gen.ts", "sdk.gen.ts", "index.ts"];
    let aguiFixed = false;
    for (const file of filesToFix) {
        const filePath = join(outputDir, file);
        try {
            const content = readFileSync(filePath, "utf-8");
            // Replace all occurrences of Agui with AGUI
            // Covers type names (AguiMessage -> AGUIMessage), method names (streamAgentAgui -> streamAgentAGUI),
            // and imports (StreamAgentAguiData -> StreamAgentAGUIData)
            const fixed = content.replace(/Agui/g, "AGUI");
            if (fixed !== content) {
                writeFileSync(filePath, fixed, "utf-8");
                aguiFixed = true;
            }
        } catch {
            // Silently ignore if file doesn't exist or can't be processed
            // This is expected for APIs that don't have AGUI types
        }
    }
    if (aguiFixed) {
        console.log(chalk.cyan("✓ Applied AGUI casing corrections"));
    }

    console.log(chalk.green("✓ TypeScript client generated successfully"));
} catch (error) {
    console.log = originalLog;
    console.error(`ERROR: Failed to generate client: ${chalk.red(error.message)}`);
    process.exit(1);
}
