# EF Core Migrations

Umbraco.Ai uses Entity Framework Core for database persistence with provider-specific migrations for SQL Server and SQLite.

## Project Structure

```
src/
├── Umbraco.Ai.Persistence/           # Core DbContext and repositories
│   ├── UmbracoAiDbContext.cs         # EF Core DbContext
│   ├── Entities/                     # Database entities
│   └── Repositories/                 # EF Core repository implementations
├── Umbraco.Ai.Persistence.SqlServer/ # SQL Server migrations
│   ├── Migrations/                   # Generated migrations
│   └── UmbracoAiDbContextFactory.cs  # Design-time factory
└── Umbraco.Ai.Persistence.Sqlite/    # SQLite migrations
    ├── Migrations/                   # Generated migrations
    └── UmbracoAiDbContextFactory.cs  # Design-time factory
```

## Creating Migrations

When you modify `UmbracoAiDbContext` or any entity classes, you need to generate new migrations for both SQL Server and SQLite.

### Generate SQL Server Migration

```bash
dotnet ef migrations add <MigrationName> -p src/Umbraco.Ai.Persistence.SqlServer -c UmbracoAiDbContext --output-dir Migrations
```

### Generate SQLite Migration

```bash
dotnet ef migrations add <MigrationName> -p src/Umbraco.Ai.Persistence.Sqlite -c UmbracoAiDbContext --output-dir Migrations
```

### Example: Adding a New Table

1. Add the entity class to `Umbraco.Ai.Persistence/Entities/`
2. Add the `DbSet<T>` property to `UmbracoAiDbContext`
3. Configure the entity in `OnModelCreating()` if needed
4. Generate migrations for both providers:

```bash
# From the repository root
dotnet ef migrations add AddNewEntity -p src/Umbraco.Ai.Persistence.SqlServer -c UmbracoAiDbContext --output-dir Migrations
dotnet ef migrations add AddNewEntity -p src/Umbraco.Ai.Persistence.Sqlite -c UmbracoAiDbContext --output-dir Migrations
```

## Removing Migrations

If you need to undo the last migration (before it's applied to any database):

```bash
# Remove SQL Server migration
dotnet ef migrations remove -p src/Umbraco.Ai.Persistence.SqlServer -c UmbracoAiDbContext

# Remove SQLite migration
dotnet ef migrations remove -p src/Umbraco.Ai.Persistence.Sqlite -c UmbracoAiDbContext
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

### umbracoAiConnection

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

### umbracoAiProfile

Stores AI profile configurations that link connections to specific models and settings.

| Column | Type | Description |
|--------|------|-------------|
| Id | GUID | Primary key |
| Alias | nvarchar(100) | Unique profile alias |
| Name | nvarchar(255) | Display name |
| Capability | int | AI capability type (Chat, Embedding, etc.) |
| ProviderId | nvarchar(100) | Provider identifier |
| ModelId | nvarchar(255) | Model identifier (e.g., "gpt-4") |
| ConnectionId | GUID | Foreign key to umbracoAiConnection |
| Temperature | float | Optional temperature setting |
| MaxTokens | int | Optional max tokens setting |
| SystemPromptTemplate | nvarchar(max) | Optional system prompt |
| TagsJson | nvarchar(2000) | JSON-serialized tags array |

## Best Practices

1. **Always generate migrations for both providers** - SQL Server and SQLite may have different syntax requirements
2. **Use descriptive migration names** - e.g., `AddUserPreferencesTable`, `AddIndexOnProfileAlias`
3. **Review generated migrations** - Check the `Up()` and `Down()` methods before committing
4. **Test migrations locally** - Run the application against both SQL Server and SQLite if possible
5. **Don't modify existing migrations** - Create new migrations for schema changes instead
