# Brief: Local-First AI Governance & Tracing for Umbraco 17

## Context

**Umbraco.AI is already implemented using Microsoft.Extensions.AI (MEAI) and Microsoft Agent Framework (MAF)**.
All AI execution, orchestration, and tool usage is therefore already aligned with Microsoft’s recommended abstractions and emits OpenTelemetry-compatible execution data.

What is missing is a **first-class Umbraco backoffice experience** that:

* makes AI behaviour visible and understandable to editors and administrators,
* supports governance, auditing, and debugging,
* works **entirely inside Umbraco by default**, and
* can later integrate with external observability platforms **without changing the implementation**.

This brief defines a **minimal, high-value scope** for such a capability.

---

## Goals

* Surface **AI execution transparency** in the Umbraco 17 backoffice.
* Provide **auditability and governance** for AI actions performed through Umbraco.AI.
* Avoid duplicating or competing with Azure Monitor, App Insights, or AI Foundry.
* Require **no external services** for baseline functionality.
* Preserve OpenTelemetry correlation so enterprise customers can opt into advanced tooling.

---

## Non-Goals (Explicit)

This initiative does **not** aim to:

* Build a general-purpose distributed tracing system.
* Recreate Azure Monitor, Application Insights, or Foundry UIs.
* Store or visualize every OpenTelemetry span or attribute.
* Provide real-time performance dashboards or cost analytics at cloud scale.

The backoffice experience is intentionally **run-centric, governance-oriented, and human-readable**.

---

## Proposed Architecture

### 1. Existing Foundation (Already in Place)

* Umbraco.AI uses **MEAI** for model abstraction and **MAF** for agentic workflows.
* AI execution already produces:

  * Trace IDs
  * Span hierarchies
  * Tool/model invocation boundaries
* This foundation is reused without modification.

---

### 2. Local AI Execution Store (New, Minimal)

Introduce a **local persistence layer** that captures a *curated subset* of AI execution data.

#### Design Principle

> Store **what is valuable for governance and debugging**, not raw telemetry.

---

## Minimal High-Value Data Scope

### Tier 1: AI Run Record (Core, Always On)

One record per AI action initiated from the Umbraco backoffice.

**Data captured**

* **Run identity**

  * RunId
  * TraceId
  * Timestamp, duration
  * Status (success / failure / cancelled)
* **Initiator context**

  * Umbraco user
  * Content/entity reference
  * Operation type (e.g. summarize, generate, classify)
* **Configuration context**

  * Model + provider
  * Agent / prompt version
* **Outcome summary**

  * Token counts (in/out, if available)
  * Error category + message (if failed)

**Why this matters**

* Enables auditing (“who did what with AI, and when”)
* Enables support and debugging without deep technical tools
* Supports governance and change tracking

This tier alone delivers most of the value.

---

### Tier 2: Execution Steps (Optional, Scoped)

Captured only when enabled (default: failures only).

**Examples**

* Tool calls (name, duration, success/failure)
* Model calls (duration, retry count)
* Exceptions with stack context (summarized)

**Explicit exclusions**

* No attempt to preserve full span trees
* No arbitrary attribute dumps
* No long-term high-volume storage

**Why this matters**

* Allows root-cause analysis of failures
* Keeps performance and storage predictable

---

## Backoffice Experience (Umbraco 17 Native)

### Extension Model

* Implemented using Umbraco 17’s **Bellissima extension registry**
* Backed by **custom Management API endpoints**
* Fully protected by backoffice authentication and authorization

---

### Minimal UI Surfaces

#### 1. AI Ops Dashboard (Admins)

* Table of recent AI runs
* Filters: status, user, operation, date
* Failure highlights
* Drill-in view per run:

  * Summary
  * Execution steps (if captured)
  * TraceId (copy/export)

#### 2. AI History on Content (Editors)

* Lightweight panel showing last N AI actions on the current item
* Status + timestamp + TraceId
* No deep diagnostics (role-appropriate visibility)

#### 3. AI Governance Settings (Admins)

* Retention (days / max runs)
* Detail level:

  * Audit only
  * Failures only
  * Sampled
* Prompt/response storage toggles
* Redaction controls
* Model / provider allowlist

---

## External Observability (Optional, Complementary)

* Local store remains authoritative for governance.
* OpenTelemetry correlation remains intact.
* Enterprise customers may optionally:

  * Export to Azure Application Insights
  * Link TraceIds to Azure AI Foundry / Monitor
* Backoffice UI can conditionally surface “View in external system” links.

---

## Default Configuration (Safe, Conservative)

* Local AI Run records: **Enabled**
* Execution steps: **Failures only**
* Retention: **7–14 days**
* Prompt/response persistence: **Off**
* External telemetry: **Disabled**

---

## Summary

This proposal builds on **existing MEAI + MAF integration in Umbraco.AI** to deliver a **local-first, governance-focused AI observability layer** tailored to Umbraco 17.

By intentionally limiting scope to **run-level auditing and failure analysis**, the solution:

* adds immediate value to editors and administrators,
* avoids infrastructure and complexity overhead,
* and remains compatible with advanced enterprise observability when needed.