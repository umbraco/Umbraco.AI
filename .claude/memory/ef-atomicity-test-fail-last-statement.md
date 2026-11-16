---
name: ef-atomicity-test-fail-last-statement
description: an EF Core rollback test must force the LAST statement to fail; EF sends deletes before updates
type: gotcha
---

A test that proves a multi-row EF Core save is atomic must make a statement fail *after* an
earlier one has succeeded. EF Core 10 orders commands on the same table by `EntityState`
(Deleted before Modified before Added), so a `BEFORE DELETE ... RAISE(ABORT)` trigger aborts the
first statement and the update is never sent. "Update not saved" then holds with no transaction
at all, and the test proves nothing.

**Why:** `RewriteMessagesAsync` (workspace-frontend-tool-duplicate-turn, T1) shipped a rollback
spec that failed the delete; review showed it stayed green without the transaction.
**How to apply:** fail the statement EF sends last (e.g. a `BEFORE UPDATE` trigger), assert the
earlier statement's effect was undone, and red-check by moving the earlier work into its own
completed scope. Two `SaveChanges` calls in one Umbraco `IEFCoreScope` are still one transaction,
so splitting saves inside the scope is not a valid red check.
