# Umbraco Documentation Vale Rules Reference

These are the 12 Vale linting rules from the official [UmbracoDocs](https://github.com/umbraco/UmbracoDocs) repository. The rules live in `.github/styles/UmbracoDocs/` and are enforced on pull requests via the `errata-ai/vale-action@reviewdog` GitHub Action with `fail_on_error: true`.

## Configuration

```ini
# .vale.ini
StylesPath = .github/styles

[*.md]
BasedOnStyles = UmbracoDocs
```

---

## Rule 1: HeadingsPunctuation (warning)

Headings must not end with punctuation (`.`, `;`, `:`, `!`, `?`).

**Pattern**: `(?:[.;:!]*)$` applied to heading scope

**Examples**:

```markdown
<!-- BAD -->
## Getting Started.
## How does it work?
## Configuration:

<!-- GOOD -->
## Getting Started
## How Does It Work
## Configuration
```

---

## Rule 2: Spacing (error)

No double spaces after punctuation. No missing space after punctuation before a capital letter.

**Patterns**:
- `[.?!,;:] {2,}` — double space after punctuation
- `[.?!,;:](?!\s)[A-Z]` — missing space after punctuation before uppercase

**Examples**:

```markdown
<!-- BAD -->
This is a sentence.  This has double space.
Run the command.Then check the output.

<!-- GOOD -->
This is a sentence. This has single space.
Run the command. Then check the output.
```

---

## Rule 3: SentenceLength (warning)

Sentences must contain fewer than 25 words.

**Scope**: sentence
**Maximum**: 25 words (counted by `\b(\w+)\b` tokens)

**Examples**:

```markdown
<!-- BAD (28 words) -->
When you are configuring the AI provider connection settings in the backoffice you need to make sure that you have the correct API key entered in the field.

<!-- GOOD (split into two) -->
Configure the AI provider connection in the backoffice. Enter the correct API key in the connection settings.
```

---

## Rule 4: ListStart (warning)

List items must start with a capital letter, unless they begin with inline code or a URL.

**Pattern**: `^(?!\[|^)[a-z].+` applied to list scope

**Examples**:

```markdown
<!-- BAD -->
- create a new connection
- navigate to the settings page

<!-- GOOD -->
- Create a new connection
- Navigate to the settings page

<!-- ALSO GOOD (inline code start) -->
- `dotnet build` compiles the solution
- [https://example.com](https://example.com) provides details
```

---

## Rule 5: Editorializing (warning)

Avoid subjective or opinionated language.

**Flagged words** (full list):

`actually`, `aptly`, `are a number`, `basically`, `clearly`, `completely`, `easy`, `easily`, `essentially`, `everybody knows`, `everyone knows`, `exceedingly`, `excellent`, `extremely`, `fairly`, `fortunately`, `huge`, `interestingly`, `is a number`, `it is important to realize`, `it should be noted that`, `just`, `largely`, `mostly`, `notably`, `note that`, `obviously`, `of course`, `quite`, `relatively`, `remarkable`, `several`, `significantly`, `simple`, `simply`, `substantially`, `surprisingly`, `tiny`, `totally`, `tragically`, `unfortunately`, `untimely`, `various`, `vast`, `very`, `without a doubt`

**Exceptions**: Technical uses like "Simple Mail Transfer Protocol" are excluded.

**Examples**:

```markdown
<!-- BAD -->
This is a simple configuration process.
You can easily set up the connection.
Just add the API key and you're done.
Obviously, you need a valid key.

<!-- GOOD -->
Configure the connection by adding the API key.
Add the API key to complete the setup.
A valid key is required.
```

---

## Rule 6: Repetition (warning)

No repeated consecutive words.

**Pattern**: Consecutive identical non-whitespace tokens

**Examples**:

```markdown
<!-- BAD -->
The the connection is configured.
You can can use any provider.

<!-- GOOD -->
The connection is configured.
You can use any provider.
```

---

## Rule 7: Acronyms (error)

All acronyms (3-5 uppercase letters) must be defined on first use in each article, unless they appear in the exceptions list.

**Definition formats accepted**:
- `ABC: Alpha Beta Charlie` (colon notation)
- `Alpha Beta Charlie (ABC)` (parenthetical)

**Exceptions** (do not need definition — ~130 total, key ones listed):

`API`, `ASP`, `CDN`, `CLI`, `CMS`, `CPU`, `CRUD`, `CSS`, `CSV`, `DNS`, `DOM`, `EOF`, `FTP`, `GIT`, `GPU`, `GUI`, `HTML`, `HTTP`, `HTTPS`, `IDE`, `IIS`, `IO`, `IP`, `JS`, `JSON`, `JWT`, `LINQ`, `MVC`, `NFS`, `NPM`, `NULL`, `ORM`, `OS`, `PDF`, `PHP`, `RAM`, `RBAC`, `REST`, `RSS`, `SDK`, `SMTP`, `SQL`, `SSD`, `SSH`, `SSL`, `SVG`, `TCP`, `TLS`, `TS`, `UDP`, `UI`, `URI`, `URL`, `USB`, `UTF`, `UX`, `VM`, `VPN`, `XML`, `XSS`, `YAML`

**Examples**:

```markdown
<!-- BAD -->
The LLM processes the request.

<!-- GOOD -->
The Large Language Model (LLM) processes the request.
<!-- or -->
LLM: Large Language Model. The LLM processes the request.
```

---

## Rule 8: Terms (warning)

Use inclusive and formal language alternatives.

| Avoid | Use Instead |
|-------|-------------|
| `master` (branch/template/view) | `primary` (branch/template/view) |
| `slave` | `secondary` |
| `blacklist` / `blacklists` | `denylist` / `denylists` |
| `whitelist` / `whitelists` | `allowlist` / `allowlists` |
| `etc` | `and so on` |
| `e.g.` | `for example` |
| `i.e.` | `that is` |
| `aka` | `also known as` |
| `docs` | `documentation` |

**Examples**:

```markdown
<!-- BAD -->
Add the domain to the whitelist, e.g. example.com, etc.

<!-- GOOD -->
Add the domain to the allowlist, for example example.com, and so on.
```

---

## Rule 9: UmbracoTerms (warning)

Enforces consistent capitalization of Umbraco-specific terms.

| Incorrect | Correct |
|-----------|---------|
| `back-office`, `back office` | `backoffice` |
| `umbraco` (lowercase) | `Umbraco` |
| `document-type`, `document type` | `Document Type` |
| `doc-type`, `doc type` | `Document Type` |
| `data-type`, `data type` | `Data Type` |
| `heartcore` | `Heartcore` |
| `umbraco cloud` | `Umbraco Cloud` |
| `umbraco deploy` | `Umbraco Deploy` |
| `umbraco forms` | `Umbraco Forms` |
| `umbraco heartcore` | `Umbraco Heartcore` |
| `umbraco cms` | `Umbraco CMS` |
| `umbraco workflow` | `Umbraco Workflow` |
| `umbraco commerce` | `Umbraco Commerce` |
| `umbraco engage` | `Umbraco Engage` |
| `umbraco ui builder` | `Umbraco UI Builder` |

**Examples**:

```markdown
<!-- BAD -->
Open the umbraco back-office and create a new document type.

<!-- GOOD -->
Open the Umbraco backoffice and create a new Document Type.
```

---

## Rule 10: Names (warning)

Enforces correct capitalization of common technology names.

| Incorrect | Correct |
|-----------|---------|
| `css` | `CSS` |
| `html` | `HTML` |
| `url` | `URL` |
| `javascript`, `js` | `JavaScript` |
| `typescript`, `ts` | `TypeScript` |
| `.net`, `dot net` | `.NET` |
| `cms` | `CMS` |
| `angularjs` (various) | `AngularJS` |

**Examples**:

```markdown
<!-- BAD -->
The frontend is built with typescript and uses css for styling.

<!-- GOOD -->
The frontend is built with TypeScript and uses CSS for styling.
```

---

## Rule 11: Brands (warning)

Enforces correct capitalization of brand names. Ignores matches inside markdown links and inline code.

**Enforced brands**: `Azure`, `Azure DevOps`, `Chrome`, `Cloudflare`, `Firefox`, `GitBook`, `GitHub`, `macOS`, `Microsoft`, `NGINX`, `Safari`, `Slack`, `Twitter`, `Umbraco ID`, `Umbraco Support`, `Visual Studio Code` (including `vs code`), `YouTube`

**Examples**:

```markdown
<!-- BAD -->
Deploy to azure using github actions.
Open the site in chrome or firefox.

<!-- GOOD -->
Deploy to Azure using GitHub Actions.
Open the site in Chrome or Firefox.
```

---

## Rule 12: LinkTextClarity (warning)

Link text must be descriptive. Generic text is flagged.

**Flagged patterns**:
- `[here]`
- `[click here]`
- `[read more]`
- `[more info]`

**Examples**:

```markdown
<!-- BAD -->
For details, [click here](https://example.com).
Learn more [here](https://example.com).

<!-- GOOD -->
For details, see the [Connection Configuration guide](https://example.com).
Learn more in the [API reference](https://example.com).
```

---

## Quick Reference Table

| # | Rule | Level | Key Check |
|---|------|-------|-----------|
| 1 | HeadingsPunctuation | warning | No `.;:!?` at end of headings |
| 2 | Spacing | **error** | Single space after punctuation |
| 3 | SentenceLength | warning | < 25 words per sentence |
| 4 | ListStart | warning | Capital letter start (unless code/URL) |
| 5 | Editorializing | warning | No subjective language |
| 6 | Repetition | warning | No consecutive duplicate words |
| 7 | Acronyms | **error** | Define on first use (except common) |
| 8 | Terms | warning | Inclusive/formal alternatives |
| 9 | UmbracoTerms | warning | Correct Umbraco term casing |
| 10 | Names | warning | Correct tech name casing |
| 11 | Brands | warning | Correct brand name casing |
| 12 | LinkTextClarity | warning | Descriptive link text |
