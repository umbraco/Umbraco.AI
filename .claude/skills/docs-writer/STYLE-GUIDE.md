# Umbraco Documentation Style Guide

This is a condensed reference of the official Umbraco documentation writing conventions, adapted for the Umbraco.AI repository. Based on the [Umbraco Style Guide](https://docs.umbraco.com/contributing/documentation/style-guide).

## Audience and Depth

**Who reads this documentation**: Umbraco developers adding AI capabilities to their CMS sites. They are comfortable with Umbraco, .NET, and web development, but may be new to AI services and this package.

**What to document**: Public APIs, NuGet/npm packages, configuration, backoffice UI, extension points, and Management API endpoints.

**What NOT to document**: Internal services, repositories, database schemas, EF Core migrations, or private implementation details. These are not part of the user contract and change without notice.

**How deep to go**: Show the reader how to achieve a task with a minimal working example. Mention that advanced options exist and link to reference material, but do not front-load complexity. A reader scanning the page should find a working code sample within the first scroll.

```markdown
<!-- TOO DEEP: explains internals the user does not need -->
The `AIProfileService` uses the `IAIProfileRepository` internally to persist
profiles to the database via EF Core. The repository calls `DbSet<AIProfile>`
on the `AIDbContext`, which supports both SQL Server and SQLite...

<!-- RIGHT DEPTH: shows what the user does -->
To retrieve a profile by ID, inject `IAIProfileService`:

```csharp
var profile = await profileService.GetProfileAsync(profileId, cancellationToken);
```
```

## Voice and Tone

### Second Person

Address the reader directly using "you" and "your". Do not use "the user", "one", or "we".

```markdown
<!-- BAD -->
The user can configure a connection in the backoffice.
One should ensure the API key is valid.
We recommend using the default profile.

<!-- GOOD -->
You can configure a connection in the backoffice.
Ensure the API key is valid.
Use the default profile for standard scenarios.
```

### Present Tense and Active Voice

Write in present tense. Prefer active voice over passive.

```markdown
<!-- BAD (past tense, passive) -->
The connection was created and the profile was assigned.
The API key will be validated when the form is submitted.

<!-- GOOD (present tense, active) -->
The system creates the connection and assigns the profile.
The form validates the API key on submission.
```

### No Editorializing

Never use subjective or opinionated language. See the full list in [VALE-RULES.md](VALE-RULES.md) Rule 5.

The most commonly violated words in technical writing:

| Do Not Use | Write Instead |
|------------|---------------|
| "simply add" | "add" |
| "just click" | "click" or "select" |
| "easy to configure" | "to configure" |
| "obviously" | (remove entirely) |
| "note that" | (restructure the sentence) |
| "basically" | (remove entirely) |

## Sentence Structure

### Length

Keep sentences under 25 words. If a sentence exceeds this, split it.

**Technique**: One idea per sentence. If you use "and" or "which" to connect clauses, consider splitting.

```markdown
<!-- BAD (32 words) -->
When you configure a new AI connection in the Umbraco backoffice you need to select the provider type and enter the API key which you can obtain from the provider dashboard.

<!-- GOOD (two sentences, 12 + 11 words) -->
Configure a new AI connection in the Umbraco backoffice. Select the provider type and enter the API key from the provider dashboard.
```

### Ambiguous Pronouns

Do not use "it" or "this" to refer to something in a previous sentence. Repeat the noun or restructure.

```markdown
<!-- BAD -->
Create a profile and assign it to a connection. It determines which model is used.

<!-- GOOD -->
Create a profile and assign the profile to a connection. The profile determines which model is used.
```

## Formatting

### Headings

- Use a single `#` title per article
- Follow hierarchical order: `#` then `##` then `###` (never skip levels)
- No punctuation at the end of headings
- Use sentence case for headings (capitalize first word and proper nouns only)

```markdown
<!-- BAD -->
# Getting Started With AI Connections:
### Configuration  (skipped ## level)

<!-- GOOD -->
# Getting started with AI connections
## Configuration
### Provider settings
```

### Lists

- **Ordered lists**: For sequential steps only. Start each item with an action verb. Maximum two actions per item.
- **Unordered lists**: For options, notes, criteria, and non-sequential items.
- All list items start with a capital letter (unless beginning with inline code or a URL).

```markdown
<!-- Ordered: sequential steps -->
1. Navigate to the AI Settings section in the backoffice.
2. Select **Connections** from the sidebar.
3. Click **Create** and choose a provider.

<!-- Unordered: options/criteria -->
- OpenAI supports chat and embedding capabilities
- Anthropic supports chat capability
- Amazon Bedrock supports chat capability
```

### Code Samples

Use fenced code blocks with language identifiers. Keep samples focused and minimal.

**Language identifiers for this repository**:

| Language | Identifier | Use For |
|----------|-----------|---------|
| C# | `csharp` | .NET backend code |
| TypeScript | `typescript` | Frontend code, Lit components |
| JSON | `json` | Configuration, API responses |
| XML | `xml` | .csproj files, NuGet config |
| SQL | `sql` | Database queries |
| Bash | `bash` | CLI commands (Linux/Mac) |
| PowerShell | `powershell` | CLI commands (Windows) |
| HTML | `html` | Markup examples |

**Code sample conventions**:

```markdown
<!-- BAD: no language, too verbose -->
```
// This is a comprehensive example showing how you might
// configure the AI service with all available options
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddUmbracoAI();
// ... many more lines
```

<!-- GOOD: language specified, focused -->
```csharp
builder.Services.AddUmbracoAI();
```
```

### Inline Formatting

- **Bold** (`**text**`): UI element names, button labels, field names
- *Italic* (`*text*`): Introducing new terms on first use
- `Code` (`` `text` ``): File names, paths, code references, CLI commands, parameter names

```markdown
Click **Create** to open the connection form. Enter the *API key* in the
`ApiKey` field. Run `dotnet build` to verify the configuration.
```

### Links

- Use descriptive link text (never "here", "click here", "read more")
- Use relative paths for internal links within the repository
- Use the article or section title as link text

```markdown
<!-- BAD -->
For more information, click [here](./configuration.md).

<!-- GOOD -->
For more information, see [Connection configuration](./configuration.md).
```

### Images

- Place images in an `images/` directory next to the referencing markdown file
- Use descriptive alt text
- Add captions where helpful
- File names: lowercase, hyphens instead of spaces

```markdown
![The AI Connections list in the Umbraco backoffice](images/ai-connections-list.png)
```

## Terminology

### Umbraco Terms

Always use the correct casing and spelling:

| Term | Notes |
|------|-------|
| Umbraco | Always capitalized |
| backoffice | One word, lowercase (unless starting a sentence) |
| Document Type | Two words, both capitalized |
| Data Type | Two words, both capitalized |
| Umbraco CMS | Product name |
| Umbraco Cloud | Product name |

### Technology Names

| Correct | Incorrect |
|---------|-----------|
| .NET | dot net, .net, dotnet (in prose) |
| TypeScript | typescript, Typescript, TS (in prose) |
| JavaScript | javascript, Javascript, JS (in prose) |
| CSS | css |
| HTML | html |
| SQL | sql (in prose) |
| EF Core | ef core, Entity Framework Core (after first use) |

### Inclusive Language

| Do Not Use | Use Instead |
|------------|-------------|
| master (branch) | primary (branch) |
| slave | secondary |
| blacklist | denylist |
| whitelist | allowlist |

### Formal Alternatives

| Do Not Use | Use Instead |
|------------|-------------|
| etc | and so on |
| e.g. | for example |
| i.e. | that is |
| aka | also known as |
| docs | documentation |

## Acronyms

Define all acronyms on first use in each article using one of these formats:

```markdown
<!-- Parenthetical -->
The Large Language Model (LLM) processes the prompt.

<!-- Colon notation -->
LLM: Large Language Model
```

Common acronyms that do not need definition: API, CLI, CMS, CSS, HTML, HTTP, HTTPS, JSON, REST, SDK, SQL, UI, URL, UX, XML. See [VALE-RULES.md](VALE-RULES.md) Rule 7 for the full exceptions list.

## File and Directory Structure

- All file and directory names use lowercase
- Use hyphens instead of spaces
- Each directory should have a `README.md` as its landing page

```
getting-started/
├── README.md
├── installation.md
├── configuration.md
└── images/
    ├── connection-form.png
    └── profile-settings.png
```

## Umbraco.AI-Specific Conventions

### Depth Guidelines by Content Type

**Installation / Getting Started**: Show the NuGet install command and the one-line DI registration. Link to configuration for next steps.

```markdown
Install the package:

```bash
dotnet add package Umbraco.AI.OpenAI
```

Register the provider in `Program.cs`:

```csharp
builder.Services.AddUmbracoAI()
    .AddOpenAI();
```
```

**Configuration**: Lead with `appsettings.json` (most common path). Mention code-based configuration exists and link to it, but do not show both inline unless the article is specifically about configuration.

```markdown
Add the connection settings to `appsettings.json`:

```json
{
  "Umbraco": {
    "AI": {
      "DefaultCapability": "chat"
    }
  }
}
```

You can also [configure options in code](./advanced-configuration.md).
```

**Management API endpoints**: Show the HTTP verb, path, and one example. Do not document every query parameter — link to the OpenAPI spec for full details.

```markdown
### Get all connections

`GET /umbraco/management/api/v1/ai/connection`

Returns all configured AI connections.
```

**Backoffice UI**: Describe the navigation path and what the user sees. Use screenshots sparingly — one per major workflow step is enough.

```markdown
1. Open the **Settings** section in the Umbraco backoffice.
2. Select **AI Connections** from the sidebar.
3. Click **Create** to add a new connection.
```

### What to Omit

Do not document:

- Internal service implementations or repository patterns
- Database schema, migrations, or EF Core details
- Private or internal classes
- DI registration internals beyond the public extension methods
- Build tooling or CI pipeline configuration (this is contributor documentation, not user documentation)
