# Settings Editor Expansion Plan

## Overview

Expand the settings editor to expose additional configuration options beyond the current default profile settings. This enables administrators to control audit logging, analytics, and security settings through the UI rather than requiring `appsettings.json` changes.

### Current State

The settings editor currently exposes only two settings via `AiSettings`:
- `DefaultChatProfileId` - Default profile for chat operations
- `DefaultEmbeddingProfileId` - Default profile for embedding operations

These are persisted using the `[AiSetting]` attribute pattern which stores settings as key-value pairs with full audit metadata.

### Goals

1. Expose audit log configuration for compliance and privacy control
2. Expose analytics settings for data retention policies
3. Expose web fetch security controls for domain whitelisting/blacklisting
4. Maintain backward compatibility with `appsettings.json` configuration
5. Settings should use UI values when set, falling back to `appsettings.json` defaults

---

## Phase 1: Audit Log Settings

### Settings to Expose

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `AuditLogEnabled` | bool | true | Enable/disable audit logging |
| `AuditLogRetentionDays` | int | 14 | Days to retain audit records |
| `AuditLogDetailLevel` | enum | FailuresOnly | Detail level (FailuresOnly/Sampled/Full) |
| `AuditLogPersistPrompts` | bool | false | Store prompt snapshots (privacy) |
| `AuditLogPersistResponses` | bool | false | Store response snapshots (privacy) |

### Implementation

**1. Add properties to `AiSettings.cs`:**
```csharp
[AiSetting]
public bool? AuditLogEnabled { get; init; }

[AiSetting]
public int? AuditLogRetentionDays { get; init; }

[AiSetting]
public AiAuditLogDetailLevel? AuditLogDetailLevel { get; init; }

[AiSetting]
public bool? AuditLogPersistPrompts { get; init; }

[AiSetting]
public bool? AuditLogPersistResponses { get; init; }
```

**2. Update `AiAuditLogOptions` to check `AiSettings` first:**
```csharp
// In AiAuditLogService or a new options resolver
public bool IsEnabled => _aiSettings.AuditLogEnabled ?? _options.Enabled;
public int RetentionDays => _aiSettings.AuditLogRetentionDays ?? _options.RetentionDays;
// etc.
```

**3. Update API models:**
- `SettingsResponseModel` - Add audit log fields
- `UpdateSettingsRequestModel` - Add audit log fields

**4. Update frontend:**
- Add audit log section to settings editor
- Toggle for enabled/disabled
- Number input for retention days
- Dropdown for detail level
- Toggles for persist prompts/responses

### Considerations

- Nullable types allow distinguishing "not set" (use appsettings default) from explicit values
- Detail level enum needs to be exposed via OpenAPI for frontend consumption
- Privacy implications of `PersistPrompts` and `PersistResponses` should be clearly communicated in UI

---

## Phase 2: Analytics Settings

### Settings to Expose

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `AnalyticsEnabled` | bool | true | Enable/disable usage analytics |
| `AnalyticsHourlyRetentionDays` | int | 30 | Days to retain hourly stats |
| `AnalyticsDailyRetentionDays` | int | 365 | Days to retain daily stats |
| `AnalyticsIncludeUserDimension` | bool | true | Track per-user usage (privacy) |

### Implementation

**1. Add properties to `AiSettings.cs`:**
```csharp
[AiSetting]
public bool? AnalyticsEnabled { get; init; }

[AiSetting]
public int? AnalyticsHourlyRetentionDays { get; init; }

[AiSetting]
public int? AnalyticsDailyRetentionDays { get; init; }

[AiSetting]
public bool? AnalyticsIncludeUserDimension { get; init; }
```

**2. Update `AiAnalyticsOptions` resolution pattern (same as audit log)**

**3. Update API models and frontend**

### Considerations

- `IncludeUserDimension` has GDPR implications - UI should explain this clearly
- Retention day limits should be validated (e.g., hourly 30-90, daily 365-730)

---

## Phase 3: Web Fetch Security Settings

### Settings to Expose

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `WebFetchEnabled` | bool | true | Enable/disable web fetch tool |
| `WebFetchAllowedDomains` | List<string> | [] | Domain whitelist (empty = allow all) |
| `WebFetchBlockedDomains` | List<string> | [] | Domain blacklist |
| `WebFetchTimeoutSeconds` | int | 30 | Request timeout |

### Implementation

**1. Add properties to `AiSettings.cs`:**
```csharp
[AiSetting]
public bool? WebFetchEnabled { get; init; }

[AiSetting]
public IReadOnlyList<string>? WebFetchAllowedDomains { get; init; }

[AiSetting]
public IReadOnlyList<string>? WebFetchBlockedDomains { get; init; }

[AiSetting]
public int? WebFetchTimeoutSeconds { get; init; }
```

**2. Update `AiWebFetchOptions` resolution pattern**

**3. Update API models and frontend**

### Considerations

- Domain list editor UI needed (add/remove domain entries)
- Validate domain format on save
- Consider wildcard support (e.g., `*.example.com`)

---

## Phase 4: Frontend Implementation

### Settings Editor Sections

Organize the settings editor into logical sections:

```
Settings
├── Defaults
│   ├── Default Chat Profile
│   └── Default Embedding Profile
├── Audit Logging
│   ├── Enabled
│   ├── Retention Days
│   ├── Detail Level
│   ├── Persist Prompts
│   └── Persist Responses
├── Analytics
│   ├── Enabled
│   ├── Hourly Retention Days
│   ├── Daily Retention Days
│   └── Include User Dimension
└── Web Fetch
    ├── Enabled
    ├── Allowed Domains
    ├── Blocked Domains
    └── Timeout Seconds
```

### UI Components Needed

1. **Section headers** - Collapsible sections with icons
2. **Toggle switches** - For boolean settings
3. **Number inputs** - With min/max validation for retention days
4. **Dropdown** - For detail level enum
5. **Domain list editor** - Add/remove domain entries with validation

### Files to Modify

- `Umbraco.Ai/src/Umbraco.Ai.Web.StaticAssets/Client/src/settings/` - Settings editor components
- `Umbraco.Ai/src/Umbraco.Ai.Web/Api/Management/Settings/Models/` - API request/response models
- OpenAPI spec regeneration after API changes

---

## Options Resolution Pattern

Create a consistent pattern for resolving settings that checks UI-configured values first, falling back to `appsettings.json`:

```csharp
public interface IOptionsResolver<TOptions> where TOptions : class
{
    TOptions Resolve();
}

public class AiAuditLogOptionsResolver : IOptionsResolver<AiAuditLogOptions>
{
    private readonly IAiSettingsService _settingsService;
    private readonly IOptions<AiAuditLogOptions> _configOptions;

    public AiAuditLogOptions Resolve()
    {
        var settings = _settingsService.GetSettingsAsync().GetAwaiter().GetResult();
        var config = _configOptions.Value;

        return new AiAuditLogOptions
        {
            Enabled = settings.AuditLogEnabled ?? config.Enabled,
            RetentionDays = settings.AuditLogRetentionDays ?? config.RetentionDays,
            DetailLevel = settings.AuditLogDetailLevel ?? config.DetailLevel,
            PersistPrompts = settings.AuditLogPersistPrompts ?? config.PersistPrompts,
            PersistResponses = settings.AuditLogPersistResponses ?? config.PersistResponses,
            // Non-UI settings remain from config only
            PersistFailureDetails = config.PersistFailureDetails,
            RedactionPatterns = config.RedactionPatterns
        };
    }
}
```

Register resolvers and update services to use them instead of `IOptions<T>` directly.

---

## Migration Considerations

- Existing `appsettings.json` configurations continue to work as defaults
- UI-set values override `appsettings.json` values
- No database migration needed - settings use existing key-value storage
- Clear documentation on precedence: UI settings > appsettings.json > defaults

---

## Priority Order

1. **Phase 1: Audit Log Settings** - High value for compliance
2. **Phase 3: Web Fetch Security** - High value for security
3. **Phase 2: Analytics Settings** - Medium value for data governance
4. **Phase 4: Frontend** - Implement progressively with each phase

---

## Files Summary

### Core Changes
- `src/Umbraco.Ai.Core/Settings/AiSettings.cs` - Add new setting properties
- `src/Umbraco.Ai.Core/Settings/AiSettingsService.cs` - May need caching updates

### Options Resolution
- New `IOptionsResolver<T>` interface and implementations
- Update services to use resolvers instead of `IOptions<T>`

### Web API
- `src/Umbraco.Ai.Web/Api/Management/Settings/Models/SettingsResponseModel.cs`
- `src/Umbraco.Ai.Web/Api/Management/Settings/Models/UpdateSettingsRequestModel.cs`

### Frontend
- `src/Umbraco.Ai.Web.StaticAssets/Client/src/settings/` - Editor components
- Regenerate OpenAPI client after API changes
