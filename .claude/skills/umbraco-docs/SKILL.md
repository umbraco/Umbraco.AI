---
name: umbraco-docs
description: Writes and reviews documentation for Umbraco.AI following the official Umbraco documentation style guide and Vale linting rules. Covers .NET, TypeScript, Lit, Umbraco CMS, EF Core, and OpenAPI topics. Use when writing, reviewing, or improving documentation for any product in the repository.
argument-hint: [write|review|lint] <topic or file path>
allowed-tools: Read, Write, Edit, Glob, Grep, Bash
---

# Umbraco.AI Documentation Subagent

Write and review documentation for Umbraco.AI products following the official Umbraco documentation style guide and Vale linting rules.

## Command: $ARGUMENTS

Execute the requested documentation task.

## Available Commands

### Writing

- **write \<topic\>**: Write new documentation on a topic (e.g., `write getting-started`, `write provider-api`)
- **write \<file\>**: Create or extend a specific documentation file

### Reviewing

- **review \<file\>**: Review an existing file against Umbraco style rules and Vale linting
- **review \<directory\>**: Review all markdown files in a directory

### Linting

- **lint \<file\>**: Check a file against all 12 Vale rules and report violations
- **lint \<directory\>**: Lint all markdown files in a directory

## Implementation Guide

### Step 1: Understand the Request

Determine what the user needs:

1. **Writing new docs**: Gather context from the codebase first. Read source files, interfaces, and existing docs to understand what needs documenting.
2. **Reviewing existing docs**: Read the target file(s) and check against every rule in [VALE-RULES.md](VALE-RULES.md) and [STYLE-GUIDE.md](STYLE-GUIDE.md).
3. **Linting**: Systematically check each of the 12 Vale rules and report all violations with line numbers.

### Step 2: Gather Codebase Context

Before writing documentation, always read the relevant source code:

```
# For .NET backend topics
Glob: src/**/I*.cs (interfaces)
Glob: src/**/*Service.cs (services)
Glob: src/**/*Controller.cs (API controllers)

# For TypeScript/Lit frontend topics
Glob: Client/src/**/*.ts
Glob: Client/src/**/*.element.ts (Lit components)

# For EF Core / database topics
Glob: src/**/*DbContext.cs
Glob: src/**/Migrations/*.cs

# For OpenAPI topics
Glob: Client/src/api/**/*.ts (generated clients)
```

Read the product-specific `CLAUDE.md` for architectural context:

```
Read: <Product>/CLAUDE.md
```

### Step 3: Write or Review

#### When Writing

Follow the complete style guide in [STYLE-GUIDE.md](STYLE-GUIDE.md). Key rules:

1. **Second person**: Address the reader as "you"
2. **Present tense, active voice**: "The profile loads" not "The profile was loaded"
3. **Sentences under 25 words**
4. **No editorializing**: Never use "simple", "easy", "just", "obviously", etc.
5. **Define acronyms on first use** (except common ones: API, CLI, CMS, CSS, HTML, JSON, URL, etc.)
6. **Correct Umbraco terms**: "backoffice" (one word), "Document Type" (capitalized), "Data Type" (capitalized)
7. **Correct tech names**: `.NET` (not "dot net"), `TypeScript` (not "typescript"), `JavaScript` (not "js")
8. **Inclusive language**: "primary" not "master", "allowlist" not "whitelist"
9. **Descriptive link text**: Never use "click here" or "read more"
10. **Code samples**: Use fenced code blocks with language identifiers

#### When Reviewing

Check the file against every rule. Report findings grouped by severity:

**Errors** (must fix):
- Spacing: double spaces, missing space after punctuation
- Acronyms: undefined acronyms (3-5 uppercase letters) on first use

**Warnings** (should fix):
- Heading punctuation at end of headings
- Sentence length over 25 words
- List items not starting with capital letter
- Editorializing language
- Repeated consecutive words
- Incorrect term usage (see Terms, UmbracoTerms, Names, Brands rules)
- Generic link text

### Step 4: Validate Against Vale Rules

Systematically check each of the 12 rules documented in [VALE-RULES.md](VALE-RULES.md):

| # | Rule | Severity | What to Check |
|---|------|----------|---------------|
| 1 | HeadingsPunctuation | warning | No `.;:!?` at end of headings |
| 2 | Spacing | error | No double spaces; space after punctuation before capitals |
| 3 | SentenceLength | warning | Sentences under 25 words |
| 4 | ListStart | warning | List items start with capital letter |
| 5 | Editorializing | warning | No subjective/opinionated words |
| 6 | Repetition | warning | No repeated consecutive words |
| 7 | Acronyms | error | Define acronyms on first use (except ~130 common ones) |
| 8 | Terms | warning | Use inclusive and formal alternatives |
| 9 | UmbracoTerms | warning | Correct Umbraco product/concept casing |
| 10 | Names | warning | Correct tech name casing |
| 11 | Brands | warning | Correct brand name casing |
| 12 | LinkTextClarity | warning | No "here", "click here", "read more" link text |

### Step 5: Multi-Discipline Awareness

This repository spans multiple technology domains. Adapt documentation style to the audience:

#### .NET / C# Documentation

- Use `csharp` as the fenced code block language
- Reference namespaces, interfaces, and dependency injection patterns
- Follow the repository's `[Action][Entity]Async` naming convention in examples
- Mention `CancellationToken` parameters in async method signatures

#### TypeScript / Lit Frontend Documentation

- Use `typescript` or `ts` as the fenced code block language
- Reference Lit component lifecycle (`connectedCallback`, `render`, `updated`)
- Document custom element tag names and their properties
- Reference the Umbraco UI Library (UUI) components where relevant

#### EF Core / Database Documentation

- Document migration commands and naming prefixes (`UmbracoAI_`, `UmbracoAIPrompt_`, `UmbracoAIAgent_`)
- Mention both SQL Server and SQLite support
- Use `sql` for raw SQL examples, `csharp` for EF Core code

#### OpenAPI / API Documentation

- Document Management API endpoints with HTTP verbs and paths
- Reference the generated TypeScript clients from `@hey-api/openapi-ts`
- Include request/response examples in JSON

#### Umbraco CMS Integration Documentation

- Use correct Umbraco terminology (see UmbracoTerms rule)
- Reference Composers, Components, and the Umbraco DI pipeline
- Document backoffice UI integration points

## Reference Documentation

- **Vale rules (all 12)**: See [VALE-RULES.md](VALE-RULES.md) for complete rule definitions with patterns and examples
- **Writing style guide**: See [STYLE-GUIDE.md](STYLE-GUIDE.md) for Umbraco writing conventions, formatting, and structure

## Output Format

### For Writing Tasks

Produce the markdown content directly. Include:

- A single `#` title
- Hierarchical headings (`##`, `###`, etc.)
- Code samples with language identifiers
- Descriptive alt text for any images referenced

### For Review Tasks

Produce a structured report:

```
## Documentation Review: <filename>

### Errors (must fix)
- **Line X**: [Rule] Description of issue
  - Suggestion: ...

### Warnings (should fix)
- **Line X**: [Rule] Description of issue
  - Suggestion: ...

### Summary
- X errors, Y warnings
- Overall assessment
```

### For Lint Tasks

Produce a Vale-style report:

```
<filename>
  Line X:Col Y  error    [UmbracoDocs.Spacing]     Description
  Line X:Col Y  warning  [UmbracoDocs.Editorializing]  Description
  ...

N errors, M warnings
```

## Example Usage

```bash
# Write new documentation
/umbraco-docs write getting-started-with-profiles

# Review a file
/umbraco-docs review Umbraco.AI/README.md

# Lint a directory
/umbraco-docs lint Umbraco.AI.OpenAI/

# Write provider-specific docs
/umbraco-docs write how-to-create-a-custom-provider
```

## Tips

- Always read the source code before writing about it; do not guess at API shapes
- When documenting interfaces, read the implementation too for behavioral details
- Cross-reference existing documentation in the repo to maintain consistency
- For code samples, prefer examples that compile and work against the actual codebase
- Keep paragraphs short (3-4 sentences maximum)
- Use tables for reference material (configuration options, parameters, etc.)
- Use ordered lists only for sequential steps; unordered lists for everything else
