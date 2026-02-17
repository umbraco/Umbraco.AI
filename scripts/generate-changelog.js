// scripts/generate-changelog.js
const fs = require("fs");
const path = require("path");
const { execSync } = require("child_process");

// Import conventional-changelog (ES module with default export)
const conventionalChangelogModule = require("conventional-changelog");
const conventionalChangelog = conventionalChangelogModule.default || conventionalChangelogModule;

// Discover products by convention
function discoverProducts(rootDir) {
    const products = {};

    // Find all directories matching Umbraco.AI* pattern
    const entries = fs.readdirSync(rootDir, { withFileTypes: true });

    for (const entry of entries) {
        if (!entry.isDirectory()) continue;
        if (!entry.name.startsWith("Umbraco.AI")) continue;

        const productDir = path.join(rootDir, entry.name);
        const configPath = path.join(productDir, "changelog.config.json");

        // Check if product has changelog config
        if (!fs.existsSync(configPath)) {
            console.warn(`⚠️  Warning: ${entry.name} has no changelog.config.json, skipping`);
            continue;
        }

        // Load product config
        try {
            const config = JSON.parse(fs.readFileSync(configPath, "utf-8"));
            products[entry.name] = {
                directory: entry.name,
                scopes: config.scopes || [],
                tagPrefix: `${entry.name}@`,
                additionalPaths: config.additionalPaths || [],
                ...config, // Allow config to override defaults
            };
        } catch (err) {
            console.error(`❌ Error loading config for ${entry.name}:`, err.message);
        }
    }

    return products;
}

// Get product config (discovers on first call, then caches)
let cachedProducts = null;
function getProducts(rootDir) {
    if (!cachedProducts) {
        cachedProducts = discoverProducts(rootDir);
    }
    return cachedProducts;
}

function getProductConfig(product, rootDir) {
    const products = getProducts(rootDir);
    const config = products[product];

    if (!config) {
        const available = Object.keys(products).join(", ");
        throw new Error(`Unknown product: ${product}. Available: ${available}`);
    }

    return config;
}

// Get previous version tag for a product
function getPreviousVersion(product, currentVersion, tagPrefix) {
    try {
        // Get all tags for this product, sorted by version
        const tags = execSync(`git tag --list "${tagPrefix}*" --sort=-version:refname`, {
            encoding: "utf-8",
        })
            .trim()
            .split("\n")
            .filter((t) => t);

        if (tags.length === 0) {
            return null; // No previous tags
        }

        // If currentVersion is provided, find the tag before it
        if (currentVersion) {
            const currentTag = `${tagPrefix}${currentVersion}`;
            const currentIndex = tags.indexOf(currentTag);

            if (currentIndex > 0) {
                return tags[currentIndex - 1];
            } else if (currentIndex === 0) {
                return null; // This is the first version
            }
        }

        // Return the most recent tag
        return tags[0];
    } catch (err) {
        console.warn(`Warning: Could not get previous version for ${product}:`, err.message);
        return null;
    }
}

async function generateChangelog(product, version, options = {}) {
    const rootDir = options.rootDir || process.cwd();
    const config = getProductConfig(product, rootDir);

    const changelogPath = path.join(rootDir, config.directory, "CHANGELOG.md");
    const tagPrefix = config.tagPrefix;

    // Get commit range
    let previousTag = options.from || getPreviousVersion(product, version, tagPrefix);

    // For unreleased mode without a previous tag, limit to recent commits to improve performance
    if (options.unreleased && !previousTag && !options.from) {
        console.log(`  No previous tags found for ${product}`);

        // Try to find the last 100 commits that touched this product's directory
        // This significantly improves performance for initial changelog generation
        try {
            const recentCommits = execSync(`git log -100 --format=%H -- "${config.directory}/"`, {
                encoding: "utf-8",
                stdio: ["pipe", "pipe", "ignore"],
            })
                .trim()
                .split("\n")
                .filter((c) => c);

            if (recentCommits.length > 0) {
                // Use the oldest of the recent commits as the starting point
                previousTag = recentCommits[recentCommits.length - 1];
                console.log(
                    `  Using last 100 commits touching ${config.directory}/ (from ${previousTag.substring(0, 7)})`,
                );
            }
        } catch (err) {
            console.warn(`  Warning: Could not get recent commits, using full history`);
        }
    }

    const fromRef = previousTag || ""; // Empty string means from beginning
    const toRef = options.to || "HEAD";

    console.log(`Generating changelog for ${product}...`);
    console.log(`  From: ${fromRef || "(beginning)"}`);
    console.log(`  To: ${toRef}`);
    console.log(`  Scopes: ${config.scopes.join(", ")}`);
    console.log(`  Note: Processing commits without scopes by checking file paths (may be slow for large histories)`);

    // Track progress for unscoped commits
    let processedCommits = 0;
    let includedCommits = 0;

    // Cache file paths for commits to avoid repeated git calls
    const commitFilesCache = new Map();

    // conventional-changelog options
    const changelogOptions = {
        preset: {
            name: "conventionalcommits",
            types: [
                { type: "feat", section: "Features" },
                { type: "fix", section: "Bug Fixes" },
                { type: "perf", section: "Performance Improvements" },
                { type: "refactor", section: "Code Refactoring" },
                { type: "docs", section: "Documentation", hidden: true },
                { type: "style", section: "Styles", hidden: true },
                { type: "chore", section: "Chores", hidden: true },
                { type: "test", section: "Tests", hidden: true },
                { type: "ci", section: "CI/CD", hidden: true },
                { type: "build", section: "Build System" },
                { type: "revert", section: "Reverts" },
            ],
        },
        tagPrefix: tagPrefix,
        // Always output unreleased when generating (we'll format the version header ourselves)
        releaseCount: 0,
        outputUnreleased: true,
    };

    // Context for template
    const context = {
        version: version,
        host: "https://github.com",
        owner: "umbraco",
        repository: "Umbraco.AI",
        repoUrl: "https://github.com/umbraco/Umbraco.AI",
        linkCompare: true,
        packageName: product,
    };

    // Git raw commits options - filter by commit range
    const gitRawCommitsOpts = {
        from: fromRef,
        to: toRef,
    };

    // Parser options
    const parserOpts = {
        mergePattern: /^Merge pull request #(\d+) from (.*)$/,
        mergeCorrespondence: ["id", "source"],
    };

    // Writer options - customize changelog output
    const writerOpts = {
        // Transform commits - filter by scope and file paths
        transform: (commit, context) => {
            processedCommits++;

            // Progress indicator every 50 commits
            if (processedCommits % 50 === 0) {
                process.stderr.write(`  Processed ${processedCommits} commits (${includedCommits} included)...\r`);
            }

            // Skip merge commits
            if (commit.merge) {
                return null;
            }

            // Strip "Co-Authored-By" lines from commit body
            if (commit.body) {
                commit.body = commit.body
                    .split('\n')
                    .filter(line => !line.trim().startsWith('Co-Authored-By:'))
                    .join('\n')
                    .trim();
            }

            // Filter by scope (primary filter)
            if (commit.scope) {
                const scopes = commit.scope.split(",").map((s) => s.trim());
                const hasMatchingScope = scopes.some((s) => config.scopes.includes(s));
                if (!hasMatchingScope) {
                    return null; // Exclude this commit
                }
            } else {
                // No scope - filter by file paths (fallback)
                // Check if commit touches product files
                if (!commit.hash) {
                    return null; // Can't check files without hash
                }

                try {
                    // Check cache first
                    let files = commitFilesCache.get(commit.hash);

                    if (!files) {
                        // Get files changed in this commit
                        files = execSync(`git diff-tree --no-commit-id --name-only -r ${commit.hash}`, {
                            encoding: "utf-8",
                            stdio: ["pipe", "pipe", "ignore"], // Suppress stderr
                        })
                            .trim()
                            .split("\n")
                            .filter((f) => f);

                        // Cache the result
                        commitFilesCache.set(commit.hash, files);
                    }

                    // Check if any file is in the product directory
                    const inProductDir = files.some((file) => {
                        // Check if file is in product directory
                        const normalizedFile = file.replace(/\\/g, "/"); // Normalize path separators
                        const normalizedDir = config.directory.replace(/\\/g, "/");

                        if (normalizedFile.startsWith(`${normalizedDir}/`)) {
                            return true;
                        }

                        // Check additional paths if configured
                        if (config.additionalPaths && config.additionalPaths.length > 0) {
                            return config.additionalPaths.some((pattern) => {
                                const normalizedPattern = pattern.replace(/\\/g, "/");
                                return normalizedFile.startsWith(normalizedPattern);
                            });
                        }

                        return false;
                    });

                    if (!inProductDir) {
                        return null; // Exclude - doesn't touch product files
                    }
                } catch (err) {
                    // If we can't determine files, exclude commit (fail closed for accuracy)
                    console.warn(`Warning: Could not get files for commit ${commit.hash}, excluding`);
                    return null;
                }
            }

            // Filter by commit type (hide certain types)
            if (commit.type === "chore" || commit.type === "ci" || commit.type === "test" || commit.type === "docs") {
                return null; // Exclude maintenance commits
            }

            // Count included commits
            includedCommits++;

            // Create a new commit object with our additions (don't modify immutable original)
            const transformedCommit = {
                ...commit,
                breaking: commit.notes && commit.notes.length > 0,
                shortHash: commit.hash ? commit.hash.substring(0, 7) : undefined,
                hashUrl: commit.hash ? `https://github.com/umbraco/Umbraco.AI/commit/${commit.hash}` : undefined,
            };

            // Process references (PR/issue links)
            // Only include numeric issue references (filter out code references like #isDisabled)
            if (transformedCommit.references && transformedCommit.references.length > 0) {
                transformedCommit.references = transformedCommit.references
                    .filter((ref) => {
                        // Only keep references that are numeric issue IDs
                        return ref.issue && /^\d+$/.test(ref.issue);
                    })
                    .map((ref) => ({
                        ...ref,
                        url: ref.issue ? `https://github.com/umbraco/Umbraco.AI/pull/${ref.issue}` : ref.url,
                    }));
            }

            return transformedCommit;
        },

        // Group commits by type
        groupBy: "type",

        // Sort commits within groups
        commitGroupsSort: "title",
        commitsSort: ["scope", "subject"],

        // Note groups for breaking changes
        noteGroupsSort: "title",
    };

    return new Promise((resolve, reject) => {
        const stream = conventionalChangelog(changelogOptions, context, gitRawCommitsOpts, parserOpts, writerOpts);
        let changelog = "";

        stream.on("data", (chunk) => {
            changelog += chunk.toString();
        });

        stream.on("end", () => {
            // Clear progress indicator
            if (processedCommits > 0) {
                process.stderr.write(`  Processed ${processedCommits} commits (${includedCommits} included)...done\n`);
            }

            // Load existing changelog if it exists
            let existingChangelog = "";
            if (fs.existsSync(changelogPath) && !options.overwrite) {
                existingChangelog = fs.readFileSync(changelogPath, "utf-8");
            }

            // Build header
            const header = `# Changelog - ${product}\n\nAll notable changes to ${product} will be documented in this file.\n\nThe format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),\nand this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).\n\n`;

            // Format the changelog section
            let formattedChangelog = "";

            if (!changelog || changelog.trim() === "") {
                // No changes found
                if (options.unreleased) {
                    formattedChangelog = "## [Unreleased]\n\nNo changes yet.\n\n";
                } else if (version) {
                    // Even with no changes, create the version section for tracking
                    const date = new Date().toISOString().split("T")[0];
                    const previousVersionTag = previousTag ? previousTag.replace(tagPrefix, "") : null;
                    const compareUrl = previousVersionTag
                        ? `https://github.com/umbraco/Umbraco.AI/compare/${tagPrefix}${previousVersionTag}...${tagPrefix}${version}`
                        : `https://github.com/umbraco/Umbraco.AI/releases/tag/${tagPrefix}${version}`;

                    formattedChangelog = `## [${version}](${compareUrl}) (${date})\n\nNo changes.\n\n`;
                }
            } else {
                // Format the changes with proper version header
                if (options.unreleased) {
                    // Keep as [Unreleased] section
                    formattedChangelog = changelog.replace(/^##\s*\[[\d.a-z-]+\].*$/m, "## [Unreleased]");
                } else if (version) {
                    // Replace the auto-generated header with proper version header
                    const date = new Date().toISOString().split("T")[0];
                    const previousVersionTag = previousTag ? previousTag.replace(tagPrefix, "") : null;
                    const compareUrl = previousVersionTag
                        ? `https://github.com/umbraco/Umbraco.AI/compare/${tagPrefix}${previousVersionTag}...${tagPrefix}${version}`
                        : `https://github.com/umbraco/Umbraco.AI/releases/tag/${tagPrefix}${version}`;

                    // Replace the header that conventional-changelog generates
                    formattedChangelog = changelog.replace(
                        /^##\s*\[[\d.a-z-]+\].*$/m,
                        `## [${version}](${compareUrl}) (${date})`,
                    );
                } else {
                    formattedChangelog = changelog;
                }
            }

            // If we have an existing changelog, handle version section replacement
            let finalChangelog = "";
            if (existingChangelog) {
                // Extract header
                const headerMatch = existingChangelog.match(/^(# Changelog[\s\S]*?)(?=\n## )/);
                const extractedHeader = headerMatch ? headerMatch[1] + "\n" : "";

                // Extract all sections after header
                const sectionsMatch = existingChangelog.match(/\n## .+[\s\S]*/);
                const sections = sectionsMatch ? sectionsMatch[0] : "";

                if (version && !options.unreleased) {
                    // Check if this version already exists in the changelog
                    const versionSectionRegex = new RegExp(
                        `\\n## \\[${version.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}\\][\\s\\S]*?(?=\\n## |$)`,
                    );

                    if (versionSectionRegex.test(sections)) {
                        // Replace existing version section
                        console.log(`  Replacing existing [${version}] section`);
                        const updatedSections = sections.replace(versionSectionRegex, "\n" + formattedChangelog.trim());
                        finalChangelog = header + updatedSections;
                    } else {
                        // Add new version section at the top
                        finalChangelog = header + formattedChangelog + sections;
                    }
                } else {
                    // For unreleased, always prepend
                    finalChangelog = header + formattedChangelog + sections;
                }
            } else {
                // No existing changelog, create new
                finalChangelog = header + formattedChangelog;
            }

            fs.writeFileSync(changelogPath, finalChangelog);
            console.log(`✅ Changelog generated at: ${changelogPath}`);
            resolve(changelogPath);
        });

        stream.on("error", (err) => {
            console.error(`❌ Error generating changelog:`, err.message);
            reject(err);
        });
    });
}

// CLI interface
if (require.main === module) {
    const args = process.argv.slice(2);
    const product = args.find((arg) => arg.startsWith("--product="))?.split("=")[1];
    const version = args.find((arg) => arg.startsWith("--version="))?.split("=")[1];
    const unreleased = args.includes("--unreleased");
    const listProducts = args.includes("--list");

    const rootDir = process.cwd();

    // List available products
    if (listProducts) {
        const products = getProducts(rootDir);
        console.log("📦 Available products:");
        Object.keys(products).forEach((p) => {
            const config = products[p];
            console.log(`  - ${p} (scopes: ${config.scopes.join(", ")})`);
        });
        process.exit(0);
    }

    if (!product) {
        console.error("❌ Error: --product is required");
        console.log("\nUsage:");
        console.log("  node scripts/generate-changelog.js --product=Umbraco.AI --version=17.1.0");
        console.log("  node scripts/generate-changelog.js --product=Umbraco.AI --unreleased");
        console.log("  node scripts/generate-changelog.js --list  # List available products");
        console.log("\nAvailable products:");
        try {
            const products = getProducts(rootDir);
            Object.keys(products).forEach((p) => console.log(`  - ${p}`));
        } catch (err) {
            console.log("  (run --list to see available products)");
        }
        process.exit(1);
    }

    generateChangelog(product, version, { unreleased, rootDir })
        .then(() => {
            console.log("✅ Done!");
            process.exit(0);
        })
        .catch((err) => {
            console.error("❌ Error:", err.message);
            process.exit(1);
        });
}

module.exports = { generateChangelog, getProducts, getProductConfig };
