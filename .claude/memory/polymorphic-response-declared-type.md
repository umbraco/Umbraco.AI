---
name: polymorphic-response-declared-type
description: Returning a [JsonPolymorphic] response model via Ok(derived) drops "$type"; set DeclaredType to the base type
type: gotcha
---

A controller that returns a `[JsonPolymorphic]` model as the root response value must set the
declared type to the polymorphic base, e.g.
`new OkObjectResult(model) { DeclaredType = typeof(DecisionResponseModel) }` or
`ActionResult<TBase>`. A plain `Ok(model)` is not enough.

**Why:** `Ok(object)` leaves `DeclaredType` null, so MVC serializes against the runtime subtype.
That subtype carries no polymorphism metadata, so System.Text.Json never writes `"$type"`. Unit
tests that call `JsonSerializer.Serialize<TBase>` directly still pass. The bug only shows up
through the real output formatter: the decision-capability-release T12 live wire check showed
`POST decision/ask` bodies with no discriminator. Polymorphic models nested as a typed property
(e.g. `ProfileResponseModel.Settings`) aren't affected.

**How to apply:** test polymorphic responses through a TestServer and the real MVC output
formatter (see `AskDecisionResponseFormattingTests`), not only with `JsonSerializer`.
