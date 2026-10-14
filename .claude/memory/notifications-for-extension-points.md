---
name: notifications-for-extension-points
description: When designing an extensible API or extension point, proactively propose matching backend notifications; cheap and an easy way for others to extend
type: decision
---

When designing or building an extensible API or extension point, propose matching backend
notification events whenever they add to the feature. Don't wait to be asked, and don't cut them by
default as "can be added later". Mirror the closest siblings' shape: a `StatefulNotification` (or
`CancelableNotification` for `*ing`) carrying `EventMessages`, published via
`IEventAggregator.PublishAsync`. For example, `AIAgentExecutingNotification` /
`AIAgentExecutedNotification`.

**Why:** notifications are cheap to add and are one of the easiest ways for developers to extend the
product. They can watch or react to a lifecycle moment without writing a full plugin or replacing a
service. In the `pluggable-agent-selection` design (01-10-2026) notifications were first cut as
YAGNI, then added back (`AIAgentSelectedNotification`) on review.

**How to apply:** in `feature-design` (and any design for a new extension point), list the lifecycle
moments the feature has (before/after, selected, executed, saved) and propose a notification for
each one that adds value. Prefer an observe-only `*ed` notification when a cancelable `*ing` one
would duplicate another override mechanism (e.g. an ordered collection that already decides the
outcome), and record why in the feature's `DECISION-LOG.md`. Add them to `ARCHITECTURE.md`'s
Connected systems table, so `feature-plan` slices them into real tasks.
