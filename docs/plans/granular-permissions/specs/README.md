# Pending specs

Each file here sits at its final repo path, below this folder. The task named in its `Skip`
reason moves it into the real test project, fills in the body from its Given/When/Then comments,
and makes it pass. The files can't live in the test projects yet: they name types that don't
exist until their task builds them.

**116 specs in 25 files**: 106 xUnit (`[Fact(Skip = "Pending implementation: T<n>")]`) and 10
Vitest (`it.skip`). The real entry point (bdd-specs rule 4) is
`Umbraco.AI.Agent/tests/Umbraco.AI.Agent.Tests.Integration/Api/AgentEndpointAccessTests.cs`, through
the test server.

**Criteria with no spec file here, proven by their `wire:` smoke test instead** (the Agent and
Prompt frontends have no test runner):

- GP-1 AC2 (a ticked right reopens ticked): T13
- GP-5 AC2 (profile picker works without Manage profiles): T13
- GP-7 AC1, AC2, AC3, AC6 (agent Access box): T27
- GP-8 AC3, AC5 (picker lists own groups, read-only field): T27
- GP-12 AC1, AC2, AC6 (prompt Access box): T34
- GP-13 AC1 (property action offers the prompt to members): T34
- GP-14 AC1, AC2 (picker in the Automate step, end to end): T39

When a task moves its last file out, delete the file here. When the folder is empty, delete it.
