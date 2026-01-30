const fs = require('fs');
const path = require('path');

// Dynamically discover and load all scopes from product config files
function loadScopes() {
  const scopes = new Set();
  const rootDir = __dirname;

  // Find all Umbraco.Ai* directories
  const entries = fs.readdirSync(rootDir, { withFileTypes: true });

  for (const entry of entries) {
    if (!entry.isDirectory()) continue;
    if (!entry.name.startsWith('Umbraco.Ai')) continue;

    const configPath = path.join(rootDir, entry.name, 'changelog.config.json');

    // Load product scopes from changelog.config.json
    if (fs.existsSync(configPath)) {
      try {
        const config = JSON.parse(fs.readFileSync(configPath, 'utf-8'));
        if (config.scopes && Array.isArray(config.scopes)) {
          config.scopes.forEach(scope => scopes.add(scope));
        }
      } catch (err) {
        console.warn(`Warning: Could not load scopes from ${configPath}:`, err.message);
      }
    }
  }

  // Add common meta scopes
  scopes.add('deps');  // Dependency updates
  scopes.add('ci');    // CI/CD changes
  scopes.add('docs');  // Documentation
  scopes.add('release');  // Release-related changes

  return Array.from(scopes).sort();
}

module.exports = {
  extends: ['@commitlint/config-conventional'],
  rules: {
    'scope-enum': [
      2,
      'always',
      loadScopes()  // Dynamically loaded from product changelog.config.json files!
    ],
    'scope-empty': [1, 'never'], // Warn if scope is missing (don't fail)
    'subject-case': [2, 'always', 'sentence-case'],
    'type-enum': [
      2,
      'always',
      ['feat', 'fix', 'docs', 'chore', 'refactor', 'test', 'perf', 'ci', 'revert', 'build']
    ]
  }
};
