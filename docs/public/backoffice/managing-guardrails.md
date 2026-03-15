---
description: >-
    Create and manage AI guardrails in the Umbraco backoffice.
---

# Managing Guardrails

AI Guardrails allow you to define safety, compliance, and quality rules that evaluate AI inputs and responses at runtime. Rules can block or warn when content is flagged.

## Accessing Guardrails

1. Navigate to **Settings** in the left sidebar
2. Expand **AI**
3. Click **Guardrails**

## Creating a Guardrail

1. Click **Create Guardrail** in the toolbar
2. Fill in the required fields:

| Field | Description                                                 |
| ----- | ----------------------------------------------------------- |
| Alias | Unique identifier for code references (URL-safe, no spaces) |
| Name  | Display name shown in the backoffice                        |

3. Click **Create**

## Adding Rules

Guardrails contain one or more rules. Each rule references a registered evaluator that performs the actual content evaluation:

1. In the guardrail editor, click **Add Rule**
2. Configure the rule:

| Field       | Description                                           |
| ----------- | ----------------------------------------------------- |
| Evaluator   | The evaluator to use (e.g., PII, Toxicity, LLM Judge) |
| Name        | Display name for the rule                             |
| Phase       | When to evaluate: Pre-Generate or Post-Generate       |
| Action      | What to do when flagged: Block or Warn                |
| Config      | Evaluator-specific settings (optional)                |

3. Click **Add**

### Evaluation Phases

| Phase            | Description                                              |
| ---------------- | -------------------------------------------------------- |
| **Pre-Generate** | Evaluates user input before sending to the AI provider   |
| **Post-Generate**| Evaluates the AI response before returning to the user   |

{% hint style="info" %}
Use Pre-Generate rules to prevent sensitive data (like PII) from being sent to AI providers. Use Post-Generate rules to validate AI responses meet your quality and safety standards.
{% endhint %}

### Actions

| Action    | Description                                                    |
| --------- | -------------------------------------------------------------- |
| **Block** | Stops processing and returns an error to the caller            |
| **Warn**  | Allows the content through but attaches warning metadata       |

### Available Evaluators

| Evaluator      | Type       | Description                                            |
| -------------- | ---------- | ------------------------------------------------------ |
| **PII**        | Code-based | Detects personal information (emails, phones, SSNs)    |
| **Toxicity**   | Code-based | Detects toxic or harmful language patterns             |
| **LLM Judge**  | Model-based| Uses an AI model to evaluate against custom criteria   |

{% hint style="info" %}
Code-based evaluators run instantly using pattern matching. Model-based evaluators call an AI model for nuanced evaluation and may take longer.
{% endhint %}

### Reordering Rules

Rules are evaluated in order. To reorder:

1. Drag rules using the handle on the left
2. Or use the arrow buttons to move rules up/down

Rules at the top are evaluated first.

## Editing a Guardrail

1. Select the guardrail from the list
2. Modify fields as needed
3. Add, edit, or remove rules
4. Click **Save**

{% hint style="info" %}
Every save creates a new version. You can view and rollback to previous versions.
{% endhint %}

## Deleting a Guardrail

1. Select the guardrail from the list
2. Click **Delete** in the toolbar
3. Confirm the deletion

{% hint style="warning" %}
Deleting a guardrail also removes all version history. Ensure the guardrail is not referenced by profiles, prompts, or agents before deletion.
{% endhint %}

## Example: Content Safety Guardrail

A typical content safety guardrail might include:

**Name**: Content Safety Policy

**Rules**:

1. **Block PII in inputs** (Pre-Generate, Block)
    - Evaluator: PII
    - Prevents personal information from being sent to AI providers

2. **Block PII in responses** (Post-Generate, Block)
    - Evaluator: PII
    - Prevents AI from generating personal information

3. **Block toxic content** (Post-Generate, Block)
    - Evaluator: Toxicity
    - Prevents harmful language in AI responses

4. **Quality check** (Post-Generate, Warn)
    - Evaluator: LLM Judge
    - Evaluates response quality against brand guidelines

## Assigning Guardrails

Guardrails are assigned to:

- **Profiles** - In the profile settings, add guardrail IDs to apply them to all requests using that profile
- **Prompts** - When editing a prompt, associate guardrails in the guardrails section (requires Prompt add-on)
- **Agents** - When configuring an agent, associate guardrails in the agent settings (requires Agent add-on)

### Assigning to a Profile

When editing a chat profile:

1. Expand the **Settings** section
2. Click **Add Guardrail**
3. Select the guardrail(s) to apply
4. Save the profile

## Version History

See [Version History](version-history.md) for information on viewing and restoring previous versions.

## Related

- [Guardrails Concept](../concepts/guardrails.md) - Understanding guardrails
- [Version History](version-history.md) - Tracking changes
