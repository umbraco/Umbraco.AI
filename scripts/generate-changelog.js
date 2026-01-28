// scripts/generate-changelog.js
const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

// Import conventional-changelog (ES module with default export)
const conventionalChangelogModule = require('conventional-changelog');
const conventionalChangelog = conventionalChangelogModule.default || conventionalChangelogModule;

// Discover products by convention
function discoverProducts(rootDir) {
  const products = {};

  // Find all directories matching Umbraco.Ai* pattern
  const entries = fs.readdirSync(rootDir, { withFileTypes: true });

  for (const entry of entries) {
    if (!entry.isDirectory()) continue;
    if (!entry.name.startsWith('Umbraco.Ai')) continue;

    const productDir = path.join(rootDir, entry.name);
    const configPath = path.join(productDir, 'changelog.config.json');

    // Check if product has changelog config
    if (!fs.existsSync(configPath)) {
      console.warn(`⚠️  Warning: ${entry.name} has no changelog.config.json, skipping`);
      continue;
    }

    // Load product config
    try {
      const config = JSON.parse(fs.readFileSync(configPath, 'utf-8'));
      products[entry.name] = {
        directory: entry.name,
        scopes: config.scopes || [],
        tagPrefix: `${entry.name}@`,
        additionalPaths: config.additionalPaths || [],
        ...config // Allow config to override defaults
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
    const available = Object.keys(products).join(', ');
    throw new Error(`Unknown product: ${product}. Available: ${available}`);
  }

  return config;
}

// Get previous version tag for a product
function getPreviousVersion(product, currentVersion, tagPrefix) {
  try {
    // Get all tags for this product, sorted by version
    const tags = execSync(`git tag --list "${tagPrefix}*" --sort=-version:refname`, {
      encoding: 'utf-8'
    }).trim().split('\n').filter(t => t);

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

  const changelogPath = path.join(rootDir, config.directory, 'CHANGELOG.md');
  const tagPrefix = config.tagPrefix;

  // Get commit range
  const previousTag = options.from || getPreviousVersion(product, version, tagPrefix);
  const fromRef = previousTag || ''; // Empty string means from beginning
  const toRef = options.to || 'HEAD';

  console.log(`Generating changelog for ${product}...`);
  console.log(`  From: ${fromRef || '(beginning)'}`);
  console.log(`  To: ${toRef}`);
  console.log(`  Scopes: ${config.scopes.join(', ')}`);

  // conventional-changelog options
  const changelogOptions = {
    preset: {
      name: 'conventionalcommits',
      types: [
        { type: 'feat', section: 'Features' },
        { type: 'fix', section: 'Bug Fixes' },
        { type: 'perf', section: 'Performance Improvements' },
        { type: 'refactor', section: 'Code Refactoring' },
        { type: 'docs', section: 'Documentation', hidden: true },
        { type: 'style', section: 'Styles', hidden: true },
        { type: 'chore', section: 'Chores', hidden: true },
        { type: 'test', section: 'Tests', hidden: true },
        { type: 'ci', section: 'CI/CD', hidden: true },
        { type: 'build', section: 'Build System' },
        { type: 'revert', section: 'Reverts' }
      ]
    },
    tagPrefix: tagPrefix,
    releaseCount: options.unreleased ? 0 : 1,
    outputUnreleased: options.unreleased
  };

  // Context for template
  const context = {
    version: version,
    host: 'https://github.com',
    owner: 'umbraco',
    repository: 'Umbraco.Ai',
    repoUrl: 'https://github.com/umbraco/Umbraco.Ai',
    linkCompare: true,
    packageName: product
  };

  // Git raw commits options - filter by commit range
  const gitRawCommitsOpts = {
    from: fromRef,
    to: toRef
  };

  // Parser options
  const parserOpts = {
    mergePattern: /^Merge pull request #(\d+) from (.*)$/,
    mergeCorrespondence: ['id', 'source']
  };

  // Writer options - customize changelog output
  const writerOpts = {
    // Transform commits - filter by scope and file paths
    transform: (commit, context) => {
      // Skip merge commits
      if (commit.merge) {
        return null;
      }

      // Filter by scope
      if (commit.scope) {
        const scopes = commit.scope.split(',').map(s => s.trim());
        const hasMatchingScope = scopes.some(s => config.scopes.includes(s));
        if (!hasMatchingScope) {
          return null; // Exclude this commit
        }
      }
      // Note: Unscoped commits are included for now
      // TODO: Add file path filtering for better accuracy (see plan for enhancement)

      // Filter by commit type (hide certain types)
      if (commit.type === 'chore' || commit.type === 'ci' || commit.type === 'test' || commit.type === 'docs') {
        return null; // Exclude maintenance commits
      }

      // Create a new commit object with our additions (don't modify immutable original)
      const transformedCommit = {
        ...commit,
        breaking: commit.notes && commit.notes.length > 0,
        shortHash: commit.hash ? commit.hash.substring(0, 7) : undefined,
        hashUrl: commit.hash ? `https://github.com/umbraco/Umbraco.Ai/commit/${commit.hash}` : undefined
      };

      // Process references (PR/issue links)
      if (transformedCommit.references && transformedCommit.references.length > 0) {
        transformedCommit.references = transformedCommit.references.map(ref => ({
          ...ref,
          url: ref.issue ? `https://github.com/umbraco/Umbraco.Ai/pull/${ref.issue}` : ref.url
        }));
      }

      return transformedCommit;
    },

    // Group commits by type
    groupBy: 'type',

    // Sort commits within groups
    commitGroupsSort: 'title',
    commitsSort: ['scope', 'subject'],

    // Note groups for breaking changes
    noteGroupsSort: 'title'
  };

  return new Promise((resolve, reject) => {
    const stream = conventionalChangelog(changelogOptions, context, gitRawCommitsOpts, parserOpts, writerOpts);
    let changelog = '';

    stream.on('data', (chunk) => {
      changelog += chunk.toString();
    });

    stream.on('end', () => {
      // Prepend to existing changelog or create new
      let existingChangelog = '';
      if (fs.existsSync(changelogPath) && !options.overwrite) {
        existingChangelog = fs.readFileSync(changelogPath, 'utf-8');

        // Remove header from existing if present
        const headerMatch = existingChangelog.match(/^# Changelog[\s\S]*?(?=\n## )/);
        if (headerMatch) {
          existingChangelog = existingChangelog.substring(headerMatch[0].length);
        }
      }

      // Build header
      const header = `# Changelog - ${product}\n\nAll notable changes to ${product} will be documented in this file.\n\nThe format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),\nand this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).\n\n`;

      // If no changes, add a note
      if (!changelog || changelog.trim() === '') {
        changelog = '## [Unreleased]\n\nNo changes yet.\n\n';
      }

      const fullChangelog = header + changelog + existingChangelog;

      fs.writeFileSync(changelogPath, fullChangelog);
      console.log(`✅ Changelog generated at: ${changelogPath}`);
      resolve(changelogPath);
    });

    stream.on('error', (err) => {
      console.error(`❌ Error generating changelog:`, err.message);
      reject(err);
    });
  });
}

// CLI interface
if (require.main === module) {
  const args = process.argv.slice(2);
  const product = args.find(arg => arg.startsWith('--product='))?.split('=')[1];
  const version = args.find(arg => arg.startsWith('--version='))?.split('=')[1];
  const unreleased = args.includes('--unreleased');
  const listProducts = args.includes('--list');

  const rootDir = process.cwd();

  // List available products
  if (listProducts) {
    const products = getProducts(rootDir);
    console.log('📦 Available products:');
    Object.keys(products).forEach(p => {
      const config = products[p];
      console.log(`  - ${p} (scopes: ${config.scopes.join(', ')})`);
    });
    process.exit(0);
  }

  if (!product) {
    console.error('❌ Error: --product is required');
    console.log('\nUsage:');
    console.log('  node scripts/generate-changelog.js --product=Umbraco.Ai --version=17.1.0');
    console.log('  node scripts/generate-changelog.js --product=Umbraco.Ai --unreleased');
    console.log('  node scripts/generate-changelog.js --list  # List available products');
    console.log('\nAvailable products:');
    try {
      const products = getProducts(rootDir);
      Object.keys(products).forEach(p => console.log(`  - ${p}`));
    } catch (err) {
      console.log('  (run --list to see available products)');
    }
    process.exit(1);
  }

  generateChangelog(product, version, { unreleased, rootDir })
    .then(() => {
      console.log('✅ Done!');
      process.exit(0);
    })
    .catch(err => {
      console.error('❌ Error:', err.message);
      process.exit(1);
    });
}

module.exports = { generateChangelog, getProducts, getProductConfig };
