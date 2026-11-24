Demo site: https://127.0.0.1:44303 · start: `cd demos/v18/Umbraco.AI.DemoSite && dotnet run --launch-profile DemoSite` (from the worktree root) · log: `/private/tmp/claude-501/-Users-matt-Documents-Work-Umbraco-Umbraco-AI/4189c2d8-dd4d-4c70-95ba-864f77657490/scratchpad/demosite.log` · login admin@example.com / password1234

# Build log: Granular permissions

Built on `v18/topic/granular-permissions-plan` (PR #779), after merging `origin/v18/dev` (7995794af).

- **T1** `ce76dcf94` Permission service. 12 specs + 2444 core unit tests green; review PASS; demo site restarts and serves with the new registration (nothing calls it until T2).
- **T2** `4afeac753` Area policies + handler. 25 handler specs (incl. all 11 policy/verb pairs) + 2469 core unit tests green; review FAIL (format) → fixed → PASS; demo site restarted, `GET profiles` unauthenticated gives 401, not 500, so the moved `SectionAccessAI` policy resolves.
