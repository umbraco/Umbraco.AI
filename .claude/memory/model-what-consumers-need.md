---
name: model-what-consumers-need
description: Before adding a concept to a contract (a type, a field, a separate list), name the consumer that needs it and what they do with it; model what's derivable as derived
type: decision
---

When designing a contract other code consumes (an API, a tool schema, a structure language), add a
concept only when a named consumer needs it to do something it otherwise can't. If it can be worked
out from what's already there, leave it out of the contract and derive it where it's needed.

**Why:** on the abilities/entities proof of concept (`experiments/abilities-poc/`, design in
`docs/plans/abilities-entities-change-sets/ARCHITECTURE.md`), the structure language grew concepts
that mirrored one product's internals rather than what its consumers (an AI, MCP, Automate) do:
"slots" apart from fields, items stored apart from other values, `@settings`/`@layout` path steps,
and nearly a separate list of which variants an entity "has". Each time, asking "who consumes this
and what do they need?" removed it: a field has a value, a path goes down through values, values
vary, and which cultures exist is product meaning worked out from the values (the CMS: cultures
with a name). The result was simpler and handled more cases (paths now reach inside Rich Text).

**How to apply:** for each new type, field or notion in a design, write down the consumer and the
action it enables. If you can't, or it can be calculated from other values, don't add it. Look at
how the platform already models it (Umbraco content, JSON Schema) before inventing a parallel shape.
