# Build Log

## v18

- **T0** `06f011dd` plan folder committed; branch pushed to `origin/v18/feature/decision-evaluators` (verified with `git ls-remote`).
- **T1** `b69d9d1e` `[AIRequiresCapability]` + `AreRequiredCapabilitiesEnabled` (object and Type overloads). 1 review round (docs overpromised execution blocking; added Inherited spec + Type overload). Reviewer rerun: unit 1306/1306, integration 32/32. Library-only; real entry point proven in T4/T5a.
- **T2** `89af05e2` Decision Safety Judge evaluator. Review PASS first round; applied 3 reviewer suggestions (pinned True/FalseCriteria, neutral Instructions wording, nullable warnings) and re-reviewed PASS. Reviewer rerun: unit 1344/1344, integration 32/32, 38/38 evaluator specs. Live check in T5a.
- **T3** `6ae4f2db` Decision Judge grader. Review PASS first round. Reviewer rerun: unit 1385/1385, integration 32/32, 41/41 grader specs. Live check in T5a.
