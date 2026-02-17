# Configuration

Deploy support provides configuration options to control how sensitive data is handled during deployment.

## Default Behavior

By default, Deploy:

- **Excludes encrypted values** - Values starting with `ENC:` are not deployed
- **Allows configuration references** - Values starting with `$` (e.g., `$OpenAI:ApiKey`) are deployed
- **Excludes sensitive fields** - Fields marked as sensitive by providers are not deployed

This ensures API keys and secrets stay safe while allowing configuration references to be deployed.

## Configuration Settings

Add these settings to your `appsettings.json`:

```json
{
  "Umbraco": {
    "AI": {
      "Deploy": {
        "Connections": {
          "IgnoreEncrypted": true,
          "IgnoreSensitive": true,
          "IgnoreSettings": []
        }
      }
    }
  }
}
```

### IgnoreEncrypted

**Default:** `true`

When `true`, blocks encrypted values (starting with `ENC:`) from being deployed, but allows configuration references (starting with `$`).

```json
"IgnoreEncrypted": true
```

**Example behavior:**

| Value in Database | Deployed? |
|-------------------|-----------|
| `$OpenAI:ApiKey` | ✅ Yes (configuration reference) |
| `ENC:abc123...` | ❌ No (encrypted value) |
| `https://api.openai.com` | ✅ Yes (plain value) |

**When to set to `false`:** Only if you need to deploy encrypted values. This is rarely needed and not recommended.

### IgnoreSensitive

**Default:** `true`

When `true`, blocks all values from fields marked as sensitive by providers, even configuration references.

```json
"IgnoreSensitive": true
```

**Example behavior:**

For a field marked as sensitive (e.g., `ApiKey`):

| Value in Database | Deployed? |
|-------------------|-----------|
| `$OpenAI:ApiKey` | ❌ No (sensitive field blocked entirely) |
| `sk-abc123...` | ❌ No (sensitive field blocked entirely) |

**When to set to `false`:** If you want to deploy configuration references for sensitive fields. Only do this if you're using configuration references for all secrets and never hardcoding API keys.

### IgnoreSettings

**Default:** `[]` (empty array)

Specify individual setting field names to always block, regardless of other settings.

```json
"IgnoreSettings": ["ApiKey", "ClientSecret"]
```

This provides fine-grained control over specific fields. Use this when you want to block specific fields but allow others.

## Filtering Priority

When Deploy evaluates whether to include a setting value, it checks in this order:

1. **IgnoreSettings** - If the field name is in this array, block it (highest priority)
2. **IgnoreSensitive** - If `true` and field is marked sensitive, block it
3. **IgnoreEncrypted** - If `true` and value starts with `ENC:`, block it
4. **Allow** - Otherwise, include the value in deployment

## Recommended Configurations

### Maximum Security (Default)

Blocks all sensitive data, only allows configuration references:

```json
{
  "Connections": {
    "IgnoreEncrypted": true,
    "IgnoreSensitive": true,
    "IgnoreSettings": []
  }
}
```

**Use when:** You want maximum protection and use configuration references for all secrets.

### Allow Configuration References for Sensitive Fields

Allows `$` references for sensitive fields:

```json
{
  "Connections": {
    "IgnoreEncrypted": true,
    "IgnoreSensitive": false,
    "IgnoreSettings": []
  }
}
```

**Use when:** You exclusively use configuration references and never hardcode API keys.

### Block Specific Fields Only

Block specific fields but allow everything else:

```json
{
  "Connections": {
    "IgnoreEncrypted": true,
    "IgnoreSensitive": false,
    "IgnoreSettings": ["ApiKey", "ClientSecret", "PrivateKey"]
  }
}
```

**Use when:** You have specific fields that should never be deployed, but other fields are safe.

## Using Configuration References

To safely deploy API keys and secrets, use configuration references:

### 1. Store Secrets in appsettings.json

**appsettings.Development.json:**
```json
{
  "OpenAI": {
    "ApiKey": "sk-dev-abc123..."
  }
}
```

**appsettings.Production.json:**
```json
{
  "OpenAI": {
    "ApiKey": "sk-prod-xyz789..."
  }
}
```

### 2. Reference in Connection Settings

When creating a Connection in the backoffice, use `$` syntax:

- **API Key field:** `$OpenAI:ApiKey`

Deploy will save this reference in the deployment file, and each environment will resolve it from its own configuration.

### 3. Verify

Check the deployment file (`.uda`) to ensure it contains the reference, not the actual key:

```json
{
  "Settings": {
    "ApiKey": "$OpenAI:ApiKey"
  }
}
```

## Environment-Specific Secrets

Use environment-specific configuration files to manage secrets:

```
appsettings.json                  # Default/shared settings
appsettings.Development.json      # Dev API keys
appsettings.Staging.json          # Staging API keys
appsettings.Production.json       # Production API keys (not in version control)
```

**Important:** Never commit `appsettings.Production.json` to version control. Use Azure Key Vault, environment variables, or other secret management solutions for production secrets.

## Troubleshooting

### API Keys Appearing in Deployment Files

If you see actual API keys in `.uda` files:

1. Verify you're using `$` references, not hardcoded keys
2. Check that `IgnoreEncrypted` is `true` (default)
3. Ensure the field is marked as sensitive by the provider

### Configuration References Not Working

If `$` references aren't resolving in target environments:

1. Verify the configuration key exists in `appsettings.json`
2. Check the configuration key path matches exactly (case-sensitive)
3. Restart the application after changing `appsettings.json`

## Next Steps

- [Deploying Entities](deploying-entities.md) - Deploy AI configuration between environments
- [Best Practices](best-practices.md) - Security and workflow recommendations
