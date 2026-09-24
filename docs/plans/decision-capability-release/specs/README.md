# Pending specs

These spec files are staged here, not in their test projects, because they reference types
that don't exist until their task lands. Placed in the real test projects, they would break
the build for every earlier task (C# test projects compile every `.cs` file in the folder).

**Each file's path under `specs/` is its final repo-relative path.** The task that makes a
file pass moves it into place, removes the `Skip`/`it.skip`, and commits it **in the same
commit as the production code** (see `umb-build-loop-gotchas`). Files a task replaces
(e.g. the spike's `ValidatingDecisionClientTests.cs`) overwrite the existing file.

Specs were written against the names in `ARCHITECTURE.md`/`SPEC.md` without compiling.
Where a builder finds a real signature differs (each file's header lists its guesses), fix
the spec to the real signature but keep the behavior it asserts and its one-assertion shape.

| Task | Files |
|------|-------|
| T2 | `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Decision/ValidatingDecisionClientTests.cs` (replaces spike file) |
| T3 | `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Services/AIProfileServiceDefaultDecisionProfileTests.cs` |
| T4 | `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Profiles/AIProfileSettingsSerializerDecisionTests.cs` |
| T5 | `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Decision/DecisionPipelineHarness.cs`, `.../Decision/AskTypedDecisionTests.cs` |
| T6 | `Umbraco.AI.TypeSafe/tests/Umbraco.AI.TypeSafe.Tests.Unit/TypeSafeProviderTests.cs`, `.../Fakes/*` |
| T8 | `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Api/Management/Capability/EnabledCapabilitiesControllerTests.cs`, `.../Provider/AllProviderControllerExperimentalTests.cs` |
| T9 | `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Api/Management/Settings/DefaultDecisionProfileSettingsTests.cs` |
| T10 | `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Api/Management/Profile/DecisionProfileSettingsMappingTests.cs` |
| T11 | `Umbraco.AI/tests/Umbraco.AI.Tests.Unit/Api/Management/Decision/AskDecisionControllerTests.cs` |
| T20 | `Umbraco.AI.Automate/tests/Umbraco.AI.Automate.Tests.Unit/Actions/DecisionActionsTests.cs` |
| T22 | `Umbraco.AI.Agent/tests/Umbraco.AI.Agent.Tests.Unit/Agents/DecisionAgentSelectionTests.cs` |

## Covered elsewhere, not by a staged spec

- **T1 / DR-11 AC3:** the on/off gating tests using `FakeDecisionCapability` already exist
  in `AIConnectionServiceTests` (~line 819) and `AIProfileServiceTests` (~line 398). T1 only
  deletes the `_ForRealJevSpikeProvider` tests, the `JevSpikeProviderId` const and the
  `Spike` using.
- **DR-1 AC14 (no usage on invalid input):** existing `AIDecisionClientFactoryTests`
  (`NeverInvokesTracker`). T2 updates it to the new types.
- **DR-2 AC16:** in `AllProviderControllerExperimentalTests` (T8), where the filter lives.
- **DR-4 AC5 (usage recorded), DR-9 AC6 (compose-time exclusion), DR-7 AC1 (create-profile
  modal):** need a real host. Proven by wire tasks T12, T21, T17.
- **DR-5 AC6, DR-11 AC1/AC2/AC4, DR-12, DR-13:** build, grep, or doc checks in their tasks'
  acceptance, not specs.

## Builder requirements the specs impose

- `TypeSafeDecisionClient` takes an injectable delay
  (`Func<TimeSpan, CancellationToken, Task>`) via an internal constructor, so retry tests
  don't sleep. Needs `InternalsVisibleTo` for the test project.
- The TypeSafe test project uses Moq + Shouldly, like the OpenAI provider tests' csproj.
- The settings-editor spec sets up the first real-element render in this Client's vitest
  suite. T15 may need to adjust that setup.
