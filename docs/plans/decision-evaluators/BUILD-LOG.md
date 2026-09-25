# Build Log

## v18

- **T0** `06f011dd` plan folder committed; branch pushed to `origin/v18/feature/decision-evaluators` (verified with `git ls-remote`).
- **T1** `b69d9d1e` `[AIRequiresCapability]` + `AreRequiredCapabilitiesEnabled` (object and Type overloads). 1 review round (docs overpromised execution blocking; added Inherited spec + Type overload). Reviewer rerun: unit 1306/1306, integration 32/32. Library-only; real entry point proven in T4/T5a.
- **T2** `89af05e2` Decision Safety Judge evaluator. Review PASS first round; applied 3 reviewer suggestions (pinned True/FalseCriteria, neutral Instructions wording, nullable warnings) and re-reviewed PASS. Reviewer rerun: unit 1344/1344, integration 32/32, 38/38 evaluator specs. Live check in T5a.
- **T3** `6ae4f2db` Decision Judge grader. Review PASS first round. Reviewer rerun: unit 1385/1385, integration 32/32, 41/41 grader specs. Live check in T5a.
- **T4** `dcf6a675` listing endpoints filter on `[AIRequiresCapability]`; obsolete ctors kept. Review PASS first round (reviewer ran the full suite 3x: unit 1401/1401, integration 32/32). Orchestrator reworded one test-collection comment per the reviewer. Live check in T5a.
- **T5a** live check on `demos/v18/` (port 44355, TypeSafe `jev-1.13.0`, key from user-secrets, never printed).
  Throwaway script `TEMP_DecisionEvaluatorsVerification.cs` in the gitignored demo site. All results as designed:
  - Flag on: `GET guardrail-evaluators` / `GET test-graders` include `decision-judge` (LLM judges still listed);
    `GET test-graders/decision-judge` 200; schema has the 3 fields, picker `capability: Decision`.
  - Evaluator via the real collection: safe text probability 0.97 → not flagged; health misinformation 0.04 →
    flagged "Safety probability 0.04 below threshold 0.70"; same with explicit `ProfileId`.
  - Real chat (`default-chat` + a guardrail with a Decision Block rule "never name a landmark"): "12 x 12" passed;
    "most famous landmark in Paris" BLOCKED by the guardrail.
  - Grader via the real collection: good 0.97 pass, bad 0.01 fail.
  - Real test runs through `IAITestRunner` (agent tests `test-content-assistant`, `test-legal-disclaimer`, cloned
    with two Decision graders): "well written" pass 0.97; "is a recipe" fail 0.00; results persisted in
    `umbracoAITestRun.GraderResultsJson`. Prompt-feature tests produced empty output because the prompt call itself
    failed (OpenAI 400 "No tool call found for function call output"), unrelated to this feature; the Decision
    grader correctly failed the empty output.
  - No default Decision profile: rule flagged / grader failed with "Default Decision profile is not configured."
  - Flag off (restart with `Umbraco__AI__Experimental__Decision=false`): both absent from listings, by-id 404;
    evaluator/grader fail safe with the "turned off" reason; the saved Block rule blocks every chat reply.
  - Backoffice (Playwright): the saved rule shows as "Decision Safety Judge"; its form shows Profile / Evaluation
    Criteria / threshold; the profile picker lists only the Decision profile.
  - Runtime flag flip without restart covered by unit specs (DE-4 AC5), not re-proven live.
- **T5** Umbraco.Docs `ai/decision-docs` local commit `b03314f6ab` (not pushed): both judges documented in 6 existing pages x 2 versions (managing-guardrails, concepts/guardrails, extending/guardrails incl. `[AIRequiresCapability]` developer note, management-api evaluators, testing-and-evaluation/graders, using-the-api/decision README). No new files. Vale clean apart from existing `guid` wording.

## v17

- **T6** v17 port on `v17/feature/decision-evaluators` (from `origin/v17/feature/decision-capability`): the 10 v18 commits cherry-picked cleanly; `c1247322` points the three obsolete notices at v19. No other CMS 17 adaptation needed. Unit 1404/1404, integration 32/32; the 5 ported spec classes 102/102.
- **T7** live check on `demos/v17/` (same script, TypeSafe `jev-1.13.0`): same results as v18 T5a. Listings on/off correct (by-id 200/404); evaluator safe 0.97 / unsafe 0.04 flagged; grader good 0.98 pass / bad 0.01 fail; real chat safe passed and rule-breaking reply BLOCKED; real agent test runs: "well written" pass 0.97, "recipe" fail 0.01 / 0.00; no default profile and flag off fail safe with the right reasons.
