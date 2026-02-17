## Umbraco.AI.Deploy

Umbraco Deploy support for Umbraco.AI - deploy AI connections, profiles, and contexts across environments with version control.

### Features

- **Connection Deployment** - Deploy AI provider connections with secure configuration references
- **Profile Deployment** - Deploy AI profiles with model configurations and dependencies
- **Context Deployment** - Deploy AI contexts with resource definitions (future feature)
- **Configuration References** - Use `$` syntax to reference appsettings values instead of storing secrets
- **Dependency Resolution** - Automatic resolution of profile-to-connection and context dependencies
- **Settings Filtering** - Three-layer filtering: IgnoreSettings, IgnoreSensitive, IgnoreEncrypted
- **Disk-Based Artifacts** - Git-friendly JSON artifacts for GitOps workflows
- **Multi-Pass Processing** - Intelligent dependency resolution during deployment

### Requirements

- Umbraco CMS 17.0.0+
- Umbraco.AI 1.0.0+
- Umbraco Deploy 17.0.0+
- .NET 10.0

### Configuration Example

```json
{
  "Umbraco": {
    "AI": {
      "Deploy": {
        "Connections": {
          "IgnoreEncrypted": true,
          "IgnoreSensitive": true
        }
      }
    }
  }
}
```
