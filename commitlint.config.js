const fs = require('fs');
const path = require('path');

// Dynamically discover and load all scopes from product config files
function loadScopes() {
  const scopes = new Set();
  const rootDir = __dirname;

  // Find all Umbraco.AI* directories
  const entries = fs.readdirSync(rootDir, { withFileTypes: true });

  for (const entry of entries) {
    if (!entry.isDirectory()) continue;
    if (!entry.name.startsWith('Umbraco.AI')) continue;

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
  scopes.add('deps');     // Dependency updates
  scopes.add('ci');       // CI/CD changes
  scopes.add('docs');     // Documentation
  scopes.add('release');  // Release-related changes
  scopes.add('hooks');    // Git hooks
  scopes.add('build');    // Build system
  scopes.add('config');   // Configuration files

  return Array.from(scopes).sort();
}

module.exports = {
  extends: ['@commitlint/config-conventional'],
  plugins: [
    {
      rules: {
        'scope-not-type': (parsed) => {
          const { type, scope } = parsed;
          if (scope && type && scope === type) {
            return [
              false,
              `Scope "${scope}" should not match type "${type}". Use just "${type}:" without a scope, or use a more specific scope like "${type}(hooks):", "${type}(build):", etc.`
            ];
          }
          return [true];
        }
      }
    }
  ],
  rules: {
    'scope-enum': [
      2,
      'always',
      loadScopes()  // Dynamically loaded from product changelog.config.json files!
    ],
    'scope-empty': [1, 'never'], // Warn if scope is missing (don't fail)
    'scope-case': [2, 'always', ['lower-case', 'kebab-case']],
    'scope-not-type': [2, 'always'],  // Enable the custom rule
    'subject-case': [2, 'always', 'sentence-case'],
    'type-enum': [
      2,
      'always',
      ['feat', 'fix', 'docs', 'chore', 'refactor', 'test', 'perf', 'ci', 'revert', 'build']
    ]
  }
};
