# EF Core Migrations

Umbraco.AI uses Entity Framework Core for database persistence with provider-specific migrations for SQL Server and SQLite.

## Project Structure

```
src/
├── Umbraco.AI.Persistence/           # Core DbContext and repositories
│   ├── UmbracoAiDbContext.cs         # EF Core DbContext
│   ├── Entities/                     # Database entities
│   └── Repositories/                 # EF Core repository implementations
├── Umbraco.AI.Persistence.SqlServer/ # SQL Server migrations
│   ├── Migrations/                   # Generated migrations
│   └── UmbracoAiDbContextFactory.cs  # Design-time factory
└── Umbraco.AI.Persistence.Sqlite/    # SQLite migrations
    ├── Migrations/                   # Generated migrations
    └── UmbracoAiDbContextFactory.cs  # Design-time factory
```

## Migration Naming Convention

All migrations MUST use the `UmbracoAi_` prefix to clearly identify them as belonging to Umbraco.AI. This is important because EF Core migrations can be used by multiple packages in the same Umbraco installation, and the prefix helps distinguish which migrations belong to which package.

**Format:** `UmbracoAi_<DescriptiveName>`

**Examples:**
- `UmbracoAi_InitialCreate`
- `UmbracoAi_AddUserPreferencesTable`
- `UmbracoAi_AddIndexOnProfileAlias`

## Creating Migrations

When you modify `UmbracoAiDbContext` or any entity classes, you need to generate new migrations for both SQL Server and SQLite.

### Generate SQL Server Migration

```bash
dotnet ef migrations add UmbracoAi_<MigrationName> -p src/Umbraco.AI.Persistence.SqlServer -c UmbracoAiDbContext --output-dir Migrations
```

### Generate SQLite Migration

```bash
dotnet ef migrations add UmbracoAi_<MigrationName> -p src/Umbraco.AI.Persistence.Sqlite -c UmbracoAiDbContext --output-dir Migrations
```

### Example: Adding a New Table

1. Add the entity class to `Umbraco.AI.Persistence/Entities/`
2. Add the `DbSet<T>` property to `UmbracoAiDbContext`
3. Configure the entity in `OnModelCreating()` if needed
4. Generate migrations for both providers:

```bash
# From the repository root
dotnet ef migrations add UmbracoAi_AddNewEntity -p src/Umbraco.AI.Persistence.SqlServer -c UmbracoAiDbContext --output-dir Migrations
dotnet ef migrations add UmbracoAi_AddNewEntity -p src/Umbraco.AI.Persistence.Sqlite -c UmbracoAiDbContext --output-dir Migrations
```

## Removing Migrations

If you need to undo the last migration (before it's applied to any database):

```bash
# Remove SQL Server migration
dotnet ef migrations remove -p src/Umbraco.AI.Persistence.SqlServer -c UmbracoAiDbContext

# Remove SQLite migration
dotnet ef migrations remove -p src/Umbraco.AI.Persistence.Sqlite -c UmbracoAiDbContext
```

## Design-Time Factories

Each migrations project contains an `IDesignTimeDbContextFactory<UmbracoAiDbContext>` implementation. These factories are **only used by EF Core CLI tools** to generate migrations - they are never called at runtime.

The connection strings in these factories are dummy values:
- SQL Server: `Server=.;Database=UmbracoAi_Design;...`
- SQLite: `Data Source=:memory:`

EF Core only needs to know which provider to use so it can generate the correct SQL syntax. No actual database connection is made during migration generation.

## Runtime Configuration

At runtime, the actual database connection is configured in `UmbracoBuilderExtensions.AddUmbracoAiPersistence()`, which:

1. Detects the database provider from Umbraco's connection string
2. Configures the appropriate `MigrationsAssembly` (SqlServer or Sqlite)
3. Applies pending migrations automatically on application startup via `RunAiMigrationNotificationHandler`

## Database Tables

The persistence layer creates the following tables:

### umbracoAIConnection

Stores AI provider connection configurations (API keys, endpoints, etc.)

| Column | Type | Description |
|--------|------|-------------|
| Id | GUID | Primary key |
| Name | nvarchar(255) | Display name |
| ProviderId | nvarchar(100) | Provider identifier (e.g., "openai") |
| SettingsJson | nvarchar(max) | JSON-serialized provider settings |
| IsActive | bit | Whether the connection is active |
| DateCreated | datetime2 | Creation timestamp |
| DateModified | datetime2 | Last modified timestamp |

### umbracoAIProfile

Stores AI profile configurations that link connections to specific models and settings.

| Column | Type | Description |
|--------|------|-------------|
| Id | GUID | Primary key |
| Alias | nvarchar(100) | Unique profile alias |
| Name | nvarchar(255) | Display name |
| Capability | int | AI capability type (Chat, Embedding, etc.) |
| ProviderId | nvarchar(100) | Provider identifier |
| ModelId | nvarchar(255) | Model identifier (e.g., "gpt-4") |
| ConnectionId | GUID | Foreign key to umbracoAIConnection |
| Temperature | float | Optional temperature setting |
| MaxTokens | int | Optional max tokens setting |
| SystemPromptTemplate | nvarchar(max) | Optional system prompt |
| TagsJson | nvarchar(2000) | JSON-serialized tags array |

## Best Practices

1. **Always use the `UmbracoAi_` prefix** - This ensures migrations are clearly identifiable (e.g., `UmbracoAi_AddUserPreferencesTable`)
2. **Always generate migrations for both providers** - SQL Server and SQLite may have different syntax requirements
3. **Use descriptive migration names** - e.g., `UmbracoAi_AddUserPreferencesTable`, `UmbracoAi_AddIndexOnProfileAlias`
4. **Review generated migrations** - Check the `Up()` and `Down()` methods before committing
5. **Test migrations locally** - Run the application against both SQL Server and SQLite if possible
6. **Don't modify existing migrations** - Create new migrations for schema changes instead
