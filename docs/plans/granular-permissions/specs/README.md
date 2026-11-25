# Pending specs

Each file here sits at its final repo path, below this folder. The task named in its `Skip`
reason moves it into the real test project, fills in the body from its Given/When/Then comments,
and makes it pass. The files can't live in the test projects yet: they name types that don't
exist until their task builds them.

**2 specs in 1 file.** Every other staged file now has a real test under another name. What is
left is `ConversationAgentAccessTests` (GP-11): "an old chat stays readable after the agent is lost"
(T35) has no real test yet. The 403 on sending is covered by `StreamConversationAutoSelectionTests`.

**Criteria with no spec file here, proven by their `wire:` smoke test instead** (the Agent and
Prompt frontends have no test runner):

- GP-1 AC2 (a ticked right reopens ticked): T13
- GP-5 AC2 (profile picker works without Manage profiles): T13
- GP-7 AC1, AC2, AC3, AC6 (agent Access box): T27
- GP-8 AC3, AC5 (picker lists own groups, read-only field): T27
- GP-12 AC1, AC2, AC6 (prompt Access box): T34
- GP-13 AC1 (property action offers the prompt to members): T34
- GP-14 AC1, AC2 (picker in the Automate step, end to end): T39
- GP-18 AC3, AC4 on the real Governance view (the list's logic has Vitest specs): T27

When a task moves its last file out, delete the file here. When the folder is empty, delete it.
