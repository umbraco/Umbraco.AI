# Changelog - Umbraco.AI

All notable changes to Umbraco.AI will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.7.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI@1.6.0...Umbraco.AI@1.7.0) (2026-03-26)

### feat

* **core,agent:** Add alias-based WithProfile and WithGuardrails to inline builders ([ff75a6d](https://github.com/umbraco/Umbraco.AI/commit/ff75a6db612e7dc153824a0764fccd7ee9e2db23))
* **core,agent:** Add ChatOptions override support for inline executions ([abf7557](https://github.com/umbraco/Umbraco.AI/commit/abf75570cebe641f01e02fbeb04f13941551fcde))
* **core:** Add AsPassThrough to inline chat builder ([f8e90d0](https://github.com/umbraco/Umbraco.AI/commit/f8e90d0dcbde1c90158039f99995a3e5b3fc5bf2))
* **core:** Add element type support to PropertyValueFormatter ([1b1788e](https://github.com/umbraco/Umbraco.AI/commit/1b1788efadb90565b023828be396c26edb58bb6f))
* **core:** Add get_content_by_route tool using IDocumentUrlService ([d7abbef](https://github.com/umbraco/Umbraco.AI/commit/d7abbef2d62805c05069ee6f59a74e257727ff5f))
* **core:** Add inline chat builder with notifications and telemetry ([95cd1a8](https://github.com/umbraco/Umbraco.AI/commit/95cd1a87100f4c348ba88c398c94d7eeb63e8f5e))
* **core:** Add read tools for copilot content access ([1473b3b](https://github.com/umbraco/Umbraco.AI/commit/1473b3b68d75713ebf4e961c7ac2f9994b95f97f))
* **core:** Improve search relevance with field boosting and phrase matching ([d8880ba](https://github.com/umbraco/Umbraco.AI/commit/d8880ba1c6b70500364086d9090157ae9a6f4138))
* **search,core:** Add Umbraco.AI.Search semantic vector search package ([dfffb84](https://github.com/umbraco/Umbraco.AI/commit/dfffb848d41449639a965da8cfde833c8c426b50))

### fix

* **core:** Distinguish IPublishedContent from IPublishedElement in formatter ([9b4bf60](https://github.com/umbraco/Umbraco.AI/commit/9b4bf600268186164176f0b78f221cb0b136d7c7))
* **core:** Filter search results by user start node permissions ([7881163](https://github.com/umbraco/Umbraco.AI/commit/788116398a1060b82e8b0eed201c2ad32ddf793c))
* **core:** Reuse existing runtime context scope in inline chat API ([6c021f4](https://github.com/umbraco/Umbraco.AI/commit/6c021f4984da3e9e7cea9760aba26d8f750e111b))

## [1.6.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI@1.5.0...Umbraco.AI@1.6.0) (2026-03-16)

### feat

* **core,agent,agent-deploy:** Generalize guardrail evaluators and promote GuardrailIds to agent level ([8371869](https://github.com/umbraco/Umbraco.AI/commit/8371869be6d3d84d8f2451f111d7086dc07e4aa9))
* **core,agent:** Add classifier chat profile setting for agent routing ([c416145](https://github.com/umbraco/Umbraco.AI/commit/c41614519de91374fcb84a26f1dea269825c17e4))
* **core,deploy:** Add guardrail Deploy support across all Deploy projects ([0527502](https://github.com/umbraco/Umbraco.AI/commit/052750206e2cecf613c686688c6d879edfd4fcf3))
* **core,deploy:** Add guardrail persistence, notifications, versioning, and tests ([c08f9ff](https://github.com/umbraco/Umbraco.AI/commit/c08f9ffd6eb6b5ec77e7e09d214d4cef40857326))
* **core,prompt,agent:** Add Governance tab to profile, prompt, and agent workspaces ([9983fdd](https://github.com/umbraco/Umbraco.AI/commit/9983fddf3f06b796230f263774ad25660d71307a))
* **core,prompt,agent:** Add guardrail integrations for Prompt, Agent, and Tests ([b3a9b04](https://github.com/umbraco/Umbraco.AI/commit/b3a9b045d5d2e33d2da3de5bc2bf4843911d15c1))
* **core,prompt,agent:** Show guardrail name in evaluation results and support delete by alias ([ff4cf0b](https://github.com/umbraco/Umbraco.AI/commit/ff4cf0bcab19e2ebd949080023b3e741dbd9c0c3))
* **core:** Add Blocked status for guardrail-blocked audit logs ([40d1032](https://github.com/umbraco/Umbraco.AI/commit/40d1032d71ddb132f81b909eed27802616ec2f27))
* **core:** Add guardrail test grader ([43e455d](https://github.com/umbraco/Umbraco.AI/commit/43e455dc187c8eeca3d35ae31676c2dfdb90c433))
* **core:** Add OpenTelemetry tracing and metrics support ([37a8164](https://github.com/umbraco/Umbraco.AI/commit/37a81641a25692313ed3b4248a282179e2221c99))
* **core:** Add Redact action and IAIGuardrailRedactable interface for guardrail evaluators ([86d5e11](https://github.com/umbraco/Umbraco.AI/commit/86d5e1125ec5bcb0817aae80ce25cc64101419b5))
* **core:** Add response guardrails system for AI safety enforcement ([bdcffc4](https://github.com/umbraco/Umbraco.AI/commit/bdcffc4e2fe4bf8d4db72f879bb1bb12f082a3ae))
* **core:** Implement redaction logic in guardrail chat client middleware ([afafa97](https://github.com/umbraco/Umbraco.AI/commit/afafa97fde53169fa161c20cfdb2ae3c3f061584))
* **core:** Log warning when guardrail rules flag content ([5452bf6](https://github.com/umbraco/Umbraco.AI/commit/5452bf65856e350b57a573ec1e85792cc5305030))
* **frontend:** Add guardrails UI feature with CRUD, rules editor, and profile integration ([04f573e](https://github.com/umbraco/Umbraco.AI/commit/04f573e30ce74082f5535e41658ee1b885ae9004))
* **ui:** Add Redact option to guardrail rule configuration UI ([db7ae70](https://github.com/umbraco/Umbraco.AI/commit/db7ae704b923a348356651b29927062446f6061d))

### fix

* **core,agent,prompt:** Fix SQLite migration NullReferenceException in Development mode ([44306b9](https://github.com/umbraco/Umbraco.AI/commit/44306b9abc7bbe1846c7b3e5a88d2487f2b9c549))
* **core,prompt,agent,copilot:** Add white-space nowrap to all uui-tag elements ([e4e9c04](https://github.com/umbraco/Umbraco.AI/commit/e4e9c04e3cbe7eb1c8782c20915032081d421f55))
* **core,prompt,agent:** Fix UI labels, noResults localization key, and regenerate API clients ([dde99b2](https://github.com/umbraco/Umbraco.AI/commit/dde99b2770c675afc8b7979a3b737f06558bdaee))

### refactor

* **core,agent,prompt:** Move ConfigureDatabaseProvider to DbContext classes ([6f086dd](https://github.com/umbraco/Umbraco.AI/commit/6f086dd17b4aedb9be0f7594e0fe40aa50ca03d8))
* **core,agent,prompt:** Remove redundant DataDirectory resolution from migration handlers ([b089bf1](https://github.com/umbraco/Umbraco.AI/commit/b089bf1b7e8da0eeaf9b2904c9cf2ca656342324))
* **core,agent,prompt:** Simplify migration handler fix and correct root cause diagnosis ([c2dc3ec](https://github.com/umbraco/Umbraco.AI/commit/c2dc3ec43f99911a61e9e3737427e5ad6ffc62b7))
* **core:** Extract shared Activity enrichment into AIActivityEnricher ([af5a90b](https://github.com/umbraco/Umbraco.AI/commit/af5a90bee2cec37866eba4dc0104a214f6c825ee))
* **core:** Remove Enabled flag from AIGuardrailRule ([3e633c2](https://github.com/umbraco/Umbraco.AI/commit/3e633c2613bc6df29a0ba4308fde366ecc1c80de))
* **core:** Rename IAIGuardrailRedactable to IAIRedactableGuardrail ([6f32635](https://github.com/umbraco/Umbraco.AI/commit/6f32635f45f42bf1fddc3be5d0a2666d815a48f1))
* **core:** Rename IAIRedactableGuardrail to IAIRedactableGuardrailEvaluator ([b2a8b4f](https://github.com/umbraco/Umbraco.AI/commit/b2a8b4f24f50411fb4a10b4a095b1018edbde989))
* **core:** Rename RedactableMatch to RedactionCandidate ([93daddb](https://github.com/umbraco/Umbraco.AI/commit/93daddb6c295134359f44f1dbfbe00f178ba0f37))
* **frontend:** Replace hand-written guardrail API with generated SDK service ([57a0dde](https://github.com/umbraco/Umbraco.AI/commit/57a0dde1359d85174962b1eeb49c4bbec670b725))
* **ui:** Reorder settings to show Classifier Chat Profile before Default Embedding Profile ([0037408](https://github.com/umbraco/Umbraco.AI/commit/00374082c243a4f2fef792c4af16115134b63180))
* **ui:** Reorganize section sidebar into Configuration and Monitoring groups ([e7a4169](https://github.com/umbraco/Umbraco.AI/commit/e7a41697e66b53cc314c814dc37c61978c91b311))

## [1.5.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI@1.4.1...Umbraco.AI@1.5.0) (2026-03-12)

### feat

* **core:** Add block entity adapter for Block List/Grid support (#87) ([58c2e71](https://github.com/umbraco/Umbraco.AI/commit/58c2e71e258dd1eeb8ca16932956db357ad27736)), closes [#87](https://github.com/umbraco/Umbraco.AI/issues/87) [#38](https://github.com/umbraco/Umbraco.AI/issues/38)
* **api:** Add remaining test run and discovery endpoints ([b3bac67](https://github.com/umbraco/Umbraco.AI/commit/b3bac671ba2893be8d0499903f1ae0b1f63e11e3))
* **api:** Add Test Management API foundation ([abdf178](https://github.com/umbraco/Umbraco.AI/commit/abdf178ff17a045d8312fdb1119f59bbf5f2a404))
* **api:** Add test run management API endpoints ([19279c5](https://github.com/umbraco/Umbraco.AI/commit/19279c5551e7b60899ddba19ee475fc564054403))
* **api:** Complete Test Management API implementation ([4d11217](https://github.com/umbraco/Umbraco.AI/commit/4d11217fe8e8547cbdb4b97de1166a9bb897507c))
* **core,frontend:** Add baseline setting and comparison UI for test runs ([18ba901](https://github.com/umbraco/Umbraco.AI/commit/18ba901ef9d9463f3acf9126cd245fb93d4a5c49))
* **core,frontend:** Add IsBaseline to run API and fix baseline action in global view ([ff8b4a7](https://github.com/umbraco/Umbraco.AI/commit/ff8b4a7207fdf8db277365d8755a2b92e5105686))
* **core,frontend:** Add mock entity editor extension point and CMS editor ([4a6cd9d](https://github.com/umbraco/Umbraco.AI/commit/4a6cd9d1b95478f5fe80a5a33c1afacc0a9c7d98))
* **core,frontend:** Add profile picker property editor and refine graders ([e28253d](https://github.com/umbraco/Umbraco.AI/commit/e28253df0bc2e4b7de0ef1f7dcf3ff65a06ec6d2))
* **core,frontend:** Enrich grader results with name, type, and metadata ([0a93598](https://github.com/umbraco/Umbraco.AI/commit/0a93598b8a1254ed8bf1856fbec044e7198d1cbc))
* **core:** Add AI Testing domain models ([e7f379f](https://github.com/umbraco/Umbraco.AI/commit/e7f379f3cde75b80bc0b318feb7f13d1796e4874))
* **core:** Add AI Testing persistence layer ([6cce34c](https://github.com/umbraco/Umbraco.AI/commit/6cce34c8683c91220e40a2b2d9ec54f2f80c0cae))
* **core:** Add AI Testing pluggability infrastructure ([77204f3](https://github.com/umbraco/Umbraco.AI/commit/77204f3fb097b040aba75226a429d222911a940a))
* **core:** Add autoSubmit option to item picker modal ([0dc09ac](https://github.com/umbraco/Umbraco.AI/commit/0dc09ac8ce8de26bd44ae62586466eac9cb5b0fd))
* **core:** Add batch execution endpoints for tests ([a95dc96](https://github.com/umbraco/Umbraco.AI/commit/a95dc965f28e5fbdca002e6ea0121601f2570591))
* **core:** Add built-in test graders for AI Testing ([bd146f7](https://github.com/umbraco/Umbraco.AI/commit/bd146f7ea8347d5d89198251889ac01fc0fbc987))
* **core:** Add collection and bulk actions for tests ([cfef40b](https://github.com/umbraco/Umbraco.AI/commit/cfef40bc4376b795f705021f6738e36dad915542))
* **core:** Add entity versioning support for AITest ([f6a19d7](https://github.com/umbraco/Umbraco.AI/commit/f6a19d75f86e4f7d2976df9e01ee1c0000882413))
* **core:** Add entity-types and sub-types API for test mock editing ([3d18c30](https://github.com/umbraco/Umbraco.AI/commit/3d18c30371d2ca321baf484a88b14aeb94dd98a0))
* **core:** Add execution and variation comparison API endpoints ([8c37bb4](https://github.com/umbraco/Umbraco.AI/commit/8c37bb46df1e1f7765a48ad5a81f21e7074d1c39))
* **core:** Add mock entity context support for test features ([f31038a](https://github.com/umbraco/Umbraco.AI/commit/f31038a3263122a116f84dd7a9a2989a49606d5e))
* **core:** Add reusable grader config builder component ([6b52e49](https://github.com/umbraco/Umbraco.AI/commit/6b52e49a61660a7481130e0aaa27f3395c87d03d))
* **core:** Add test feature entity picker infrastructure ([26e3273](https://github.com/umbraco/Umbraco.AI/commit/26e327371e0b45d4b5330121a131e894eb7ba5b4))
* **core:** Add test framework database migrations ([84438ef](https://github.com/umbraco/Umbraco.AI/commit/84438ef172ffc2b6447375bfe50228dc68b33c41))
* **core:** Add test navigation system and workspace infrastructure ([dedc668](https://github.com/umbraco/Umbraco.AI/commit/dedc6680e942591c954bba91e3952efef7cea02d))
* **core:** Add test run service for run management and comparison ([41da0b9](https://github.com/umbraco/Umbraco.AI/commit/41da0b9780643665ed62f3a020e956a0f0740202))
* **core:** Add test run transcript API and tabbed detail modal ([abbfc25](https://github.com/umbraco/Umbraco.AI/commit/abbfc2521336ae63dab83bd0c1ddc0e8fc72ca60))
* **core:** Add test service layer for AI Testing ([ef85cc4](https://github.com/umbraco/Umbraco.AI/commit/ef85cc4569646a6d8300aaa293b1e9c1b01faf27))
* **core:** Add test variation domain models ([1934767](https://github.com/umbraco/Umbraco.AI/commit/19347671b2eb576c6cad9f78e27977cc72bc819a))
* **core:** Add test variation persistence, API models, and migrations ([cddb5dd](https://github.com/umbraco/Umbraco.AI/commit/cddb5ddfb0e855e828f5a4e7c0d3e3ec560127e6))
* **core:** Add variation-aware test execution and result aggregation ([3771c1e](https://github.com/umbraco/Umbraco.AI/commit/3771c1e6dde0441a304be2fb20df311f96abf25e))
* **core:** Implement multi-run execution with pass@k metrics ([acc9894](https://github.com/umbraco/Umbraco.AI/commit/acc9894a8feebb0c03f927d6612e52c015c24dfe))
* **core:** Improve test case editor layout and organization ([3a8ad81](https://github.com/umbraco/Umbraco.AI/commit/3a8ad8197a8cbf892d4583f3d72a055d13a98f3b))
* **core:** Integrate test feature entity picker into test workspace ([678f7fa](https://github.com/umbraco/Umbraco.AI/commit/678f7facd19cf92aa76806e12959bd233ed26193))
* **core:** Keep grader picker open when editing config ([4251ba4](https://github.com/umbraco/Umbraco.AI/commit/4251ba494188039a03e7d0b46401365058f75197))
* **core:** Make TestFeatureId init-only on AITest with UI feature selection ([61af760](https://github.com/umbraco/Umbraco.AI/commit/61af76064ed675117263ad39a5537235ab41906f))
* **core:** Use model editor with schema for test case configuration ([17fc0e5](https://github.com/umbraco/Umbraco.AI/commit/17fc0e5475c43b29fd2f1ee88fc056981936b3b4))
* **frontend,prompt:** Add entity picker and entity property picker editors ([1a78156](https://github.com/umbraco/Umbraco.AI/commit/1a78156823a4292243994aaf8904d234876c80ff))
* **frontend:** Add AI Tests workspace UI components ([bca84a6](https://github.com/umbraco/Umbraco.AI/commit/bca84a6e07aa9faddf7d78724ca6eb2f24b9c61c))
* **frontend:** Add delete entity action to test run collection rows ([27e5e58](https://github.com/umbraco/Umbraco.AI/commit/27e5e5852036b5e16c5da94adeea7e96dc686d6e))
* **frontend:** Add execution grouping to test run table ([a619da0](https://github.com/umbraco/Umbraco.AI/commit/a619da045abb98172faf4404302c8aa36ff94fc9))
* **frontend:** Add execution summary modal with metrics comparison ([1698d80](https://github.com/umbraco/Umbraco.AI/commit/1698d80152a2ef99d79e8b394a5c16380c451703))
* **frontend:** Add Info workspace view to test entity ([4b9cc57](https://github.com/umbraco/Umbraco.AI/commit/4b9cc57f44c5aaee1b796e6ceccba0ee078aa15f))
* **frontend:** Add JSON editor button to mock entity summary card ([5279fb0](https://github.com/umbraco/Umbraco.AI/commit/5279fb0d12571bb6c56d35299481b6aa25d7061c))
* **frontend:** Add run button to test workspace editor ([ceebd11](https://github.com/umbraco/Umbraco.AI/commit/ceebd11e8940531f7a3126dffbd8adeafbf24a8d))
* **frontend:** Add search filter to test runs collection view ([30a503d](https://github.com/umbraco/Umbraco.AI/commit/30a503d6e9ea0a9690a68f45d2cdc348dc00d4ad))
* **frontend:** Add test run UI infrastructure ([5e77c61](https://github.com/umbraco/Umbraco.AI/commit/5e77c61e87d85f6cc7eb41bb0dc8f46c88e738d7))
* **frontend:** Add test variations UI with configuration tab and variation builder ([f549303](https://github.com/umbraco/Umbraco.AI/commit/f549303e4fb31a694761217c9ce356d38e118179))
* **frontend:** Add TestEntityContext composite property editor ([5a626b2](https://github.com/umbraco/Umbraco.AI/commit/5a626b2bdcc9764da8ca526fc7df8a864304cfa9))
* **frontend:** Add View Details entity action for test runs ([09d7e11](https://github.com/umbraco/Umbraco.AI/commit/09d7e11914d8e39cde7530cf4bfd0031dfb9f3cb))
* **frontend:** Display Pass@K scores in test run views ([c7ae06e](https://github.com/umbraco/Umbraco.AI/commit/c7ae06efc79e6d620d04f17f27a772ecaf552bbf))
* **frontend:** Show best variation indicator in execution summary ([206e320](https://github.com/umbraco/Umbraco.AI/commit/206e320b0245b97a106230f0dee9e4c7459b4c34))
* TipTap AI prompt integration with selection support (#86) ([e0c3f24](https://github.com/umbraco/Umbraco.AI/commit/e0c3f24f9f9d95497a3d71c61ecc7f8b59e08453)), closes [#86](https://github.com/umbraco/Umbraco.AI/issues/86)

### fix

* **core,agent:** Resolve build errors after dev merge ([75ff4c1](https://github.com/umbraco/Umbraco.AI/commit/75ff4c1e33b0e9cc844aa643613adc975c27b6ae))
* **core,deploy-prompt,deploy-agent:** Fix failing unit tests ([a0efc8b](https://github.com/umbraco/Umbraco.AI/commit/a0efc8b9c2d12d2021a022050f097e254f97bf3e))
* **core,frontend:** Add baselineRunId to test run response for comparison panel ([0084795](https://github.com/umbraco/Umbraco.AI/commit/0084795af05719d046aba8ba80bbd986565f43f6))
* **core,frontend:** Update frontend for strong type API changes ([0d38b75](https://github.com/umbraco/Umbraco.AI/commit/0d38b752c6fdbe0bfb1b7e66bf96d4d58b40bbf5))
* **core,openai,anthropic,google,microsoft-foundry:** Fix graders and update provider packages ([087a132](https://github.com/umbraco/Umbraco.AI/commit/087a1327d18412e64502c0ac780a8b4b2343cbec))
* **core,prompt,agent:** Fix test feature config deserialization and transcript accuracy ([a0205c4](https://github.com/umbraco/Umbraco.AI/commit/a0205c4e9e0631e3857f6168e4318514fc947228))
* **core,prompt,agent:** Fix test feature repository matching ([6481cbf](https://github.com/umbraco/Umbraco.AI/commit/6481cbf6ca689465aaf58b924403932ede9d3258))
* **core:** Add createScaffold method to test detail repository ([da0d37d](https://github.com/umbraco/Umbraco.AI/commit/da0d37d6279b81db1c13e19d9a4776ea56ee378f))
* **core:** Add null checks for modal manager in grader config builder ([8ea1dfd](https://github.com/umbraco/Umbraco.AI/commit/8ea1dfd0959bb1a497e7bc5339a897b3f9d197f5))
* **core:** Correct test workspace context token definition ([9a4e00a](https://github.com/umbraco/Umbraco.AI/commit/9a4e00a93e937bec53739bd715908f1ee593c1fd))
* **core:** Fix build errors from formatter-to-adapter migration ([73530af](https://github.com/umbraco/Umbraco.AI/commit/73530af9508465624fe708e290be887359dc87c0))
* **core:** Fix compilation errors after dev merge ([6767856](https://github.com/umbraco/Umbraco.AI/commit/6767856dece406c1e5fc7a102b4d8c82a5c657a7))
* **core:** Fix frontend TypeScript errors and template issues ([dab37de](https://github.com/umbraco/Umbraco.AI/commit/dab37de2c82d7c3ea5ed55756cc8e0c95212dcf8))
* **core:** Fix test feature mapper type resolution ([20edae9](https://github.com/umbraco/Umbraco.AI/commit/20edae94ba3ca32ca08a5519a7c5ca04c70d25b5))
* **core:** Merge composition containers and match blueprint layout ([6ac1fa6](https://github.com/umbraco/Umbraco.AI/commit/6ac1fa6fcc4f94bcd660b51a2737542c6fd5cdb1))
* **core:** Persist test transcript to database during test execution ([9c6634c](https://github.com/umbraco/Umbraco.AI/commit/9c6634c95ce10cfc705cf39cb1e34bd1c9771b86))
* **core:** Register test framework repository implementations ([4d30092](https://github.com/umbraco/Umbraco.AI/commit/4d30092214288a6b87d52099b2b21300ea8f7708))
* **core:** Register TestMapDefinition with Umbraco mapper ([3372e1a](https://github.com/umbraco/Umbraco.AI/commit/3372e1a55202b9e893d7308fabbf6061e3c17350))
* **core:** Remove editor element from workspace view manifests ([532b968](https://github.com/umbraco/Umbraco.AI/commit/532b9688a6b541d952259770d0e04d37171ad18e))
* **core:** Remove unused imports from test workspace editor ([a7d4025](https://github.com/umbraco/Umbraco.AI/commit/a7d4025a097d6d5fe43cc211e0e1c0645c29afe3))
* **core:** Resolve grader names from test config in run comparison ([72e2a87](https://github.com/umbraco/Umbraco.AI/commit/72e2a873f5d444d2fda9d3589ad7661757fe6e66))
* **core:** Set all required properties in UpdateTestRequestModel mapper factory ([d5daec1](https://github.com/umbraco/Umbraco.AI/commit/d5daec1482be2af224e850dc524283e362013820))
* **core:** Update all graders to use new AIField API ([6e7b58b](https://github.com/umbraco/Umbraco.AI/commit/6e7b58b596a87dd0a9b1738770cecfa93994da08))
* **core:** Use detail repository with observable pattern for test workspace ([c25c22f](https://github.com/umbraco/Umbraco.AI/commit/c25c22f5294e81c81c4701fb0c86e078f55d6138))
* **core:** Use explicit source type in mapper call ([e22649a](https://github.com/umbraco/Umbraco.AI/commit/e22649a12f0392e907af9f2b1301e833b088cfd1))
* **frontend:** Add clickable link on run text to open detail modal ([96dfe7e](https://github.com/umbraco/Umbraco.AI/commit/96dfe7e8a6d1bf4ecde34db1a8dc097c27805e5a))
* **frontend:** Add context picker to configuration tab and variation editor ([ff8017f](https://github.com/umbraco/Umbraco.AI/commit/ff8017f8cdfe5852f71cceacbb0036d44ff0b938))
* **frontend:** Add placeholder option to entity type picker ([db61704](https://github.com/umbraco/Umbraco.AI/commit/db61704230b0aab78aca11319fec6f18a2577250))
* **frontend:** Fix execution summary modal layout ([b60bcaa](https://github.com/umbraco/Umbraco.AI/commit/b60bcaa01c461c70609f80f43b95dc96b85a7380))
* **frontend:** Fix modal header and tab container overflow ([b2fac97](https://github.com/umbraco/Umbraco.AI/commit/b2fac97f4f1e704d7125c2c02a2e852ee79fab74))
* **frontend:** Fix model editor change handler in grader config ([1912e68](https://github.com/umbraco/Umbraco.AI/commit/1912e68709ea7079e8d9508cac187a2e59d1bcde))
* **frontend:** Fix sub-types not showing on initial load ([90bdb54](https://github.com/umbraco/Umbraco.AI/commit/90bdb54242cd9cf16c3ccd9801cdbd3f27698277))
* **frontend:** Fix sub-types not showing on initial load ([b7ffe72](https://github.com/umbraco/Umbraco.AI/commit/b7ffe72ca3fb07a24887699b294e46362d0cc56c))
* **frontend:** Fix uui-select usage and use uui-toggle for negate ([d1e4e64](https://github.com/umbraco/Umbraco.AI/commit/d1e4e6470f3e20b9275a8370617dfb44e0120143))
* **frontend:** Hide mock entity editor until selections are complete ([19cb83a](https://github.com/umbraco/Umbraco.AI/commit/19cb83a3744757043368a4c4a4b5b6aba95ce1fa))
* **frontend:** Improve grader summary to avoid name duplication ([9bf29d3](https://github.com/umbraco/Umbraco.AI/commit/9bf29d37bee13912b6b66e4754024158a67dde70))
* **frontend:** Keep grader type picker open when config editor is shown ([3aae25a](https://github.com/umbraco/Umbraco.AI/commit/3aae25ab7076e2a0e27d2428b35f5a37e2a02066))
* **frontend:** Move editor header into sticky area matching blueprint ([a2321a9](https://github.com/umbraco/Umbraco.AI/commit/a2321a96c9224eff8581b6d34b129482b829fee1))
* **frontend:** Polish test run table column order and detail modal ([38bff4c](https://github.com/umbraco/Umbraco.AI/commit/38bff4c8ae311392f87c5df8071d2d89335af0f3))
* **frontend:** Rebuild table items when baselineRunId changes ([219c891](https://github.com/umbraco/Umbraco.AI/commit/219c891b97b3db2f842ccd0aece129b4d62478cf))
* **frontend:** Refine execution column in test run table ([2fcc95c](https://github.com/umbraco/Umbraco.AI/commit/2fcc95cb33faf83255a276f04d90391c97569a7d))
* **frontend:** Refresh test run collection after test execution ([f6b0c20](https://github.com/umbraco/Umbraco.AI/commit/f6b0c20562e8fd856bc680bdc20d31568ef61b99))
* **frontend:** Remove border-radius from transcript message styling ([3ab0168](https://github.com/umbraco/Umbraco.AI/commit/3ab0168f2331ea0b78289453ceb24d25fb85b0c2))
* **frontend:** Restore default uui-box padding in execution summary ([aacdcbd](https://github.com/umbraco/Umbraco.AI/commit/aacdcbd52adce17b45a58c27a2ccfd3b47993eb1))
* **frontend:** Show full test ID in test run table view ([f26faa1](https://github.com/umbraco/Umbraco.AI/commit/f26faa116c29d1e2ea848fa81c440e47763ca8ed))
* **frontend:** Show trophy for all tied best variations ([485bc1f](https://github.com/umbraco/Umbraco.AI/commit/485bc1f03cd52740e36a539a57b2bf18a37a89ad))
* **frontend:** Show underline on run detail link ([42b8181](https://github.com/umbraco/Umbraco.AI/commit/42b818146dde0f20a2380a4318a2ae5c60cdf0e8))
* **frontend:** Simplify bulk delete confirm and refresh collection after delete ([79a1718](https://github.com/umbraco/Umbraco.AI/commit/79a1718dd218562b2c2cddb1fd73fc8a127eb959))
* **frontend:** Stop event propagation on run detail link click ([d4c3cfb](https://github.com/umbraco/Umbraco.AI/commit/d4c3cfb2bb06ee5062027c8f49b9e4f0bea2583b))
* **frontend:** Update test workspace Settings view configuration ([962a60e](https://github.com/umbraco/Umbraco.AI/commit/962a60e32a935efefa4d3a232d3e0be0e8ed036b))
* **frontend:** Use clickable Run ID link for run detail modal ([a7c636f](https://github.com/umbraco/Umbraco.AI/commit/a7c636fc1ceb56c40982b5dcc491a792b26b5841))
* **frontend:** Use generated SDK for entity-types API calls ([3ce31fb](https://github.com/umbraco/Umbraco.AI/commit/3ce31fb37fbdebc7559a7281b8ece000e3b95476))
* **frontend:** Use inline styles for table cell content ([29bdbac](https://github.com/umbraco/Umbraco.AI/commit/29bdbacd24a90f25cf3af2db8bf425a6cd1f10ba))
* **frontend:** Use partial update instead of full reload for set baseline action ([90d790e](https://github.com/umbraco/Umbraco.AI/commit/90d790e466167919ed0fb1f28e9b8d54a03f86bb))
* **frontend:** Use SDK TestsService for execution result endpoint ([93305db](https://github.com/umbraco/Umbraco.AI/commit/93305dbc1c7a1ceec6ff681204185c076b05810f))
* **frontend:** Use uui-box for test run detail sections ([838bbfb](https://github.com/umbraco/Umbraco.AI/commit/838bbfbf2478994477f3c99ed9af34d7b3024bec))

### perf

* **core:** Optimize test run service queries for better database performance ([68896ef](https://github.com/umbraco/Umbraco.AI/commit/68896ef33025e5dc8910c8cccadc90558aacebdc))

### refactor

* **core,agent,prompt:** Standardize pagination to use tuples ([a9eb52c](https://github.com/umbraco/Umbraco.AI/commit/a9eb52ca613a4ef0ecc6dd3cbede50f3ac53706d))
* **core,agent,prompt:** Use JsonElement for test case data ([dd5c2f0](https://github.com/umbraco/Umbraco.AI/commit/dd5c2f02be1c024bc1ad804c348edf080c47dcac))
* **core,api,frontend:** Migrate tests to unified AI section ([b801b02](https://github.com/umbraco/Umbraco.AI/commit/b801b02d805dff184cf1ebba0f7bdfc26907b010))
* **core,frontend:** Rename TestCase to TestFeatureConfig and integrate serializer ([67b1412](https://github.com/umbraco/Umbraco.AI/commit/67b14122d4fad7389e8447ce5eb41e37166c80c5))
* **core,prompt,agent:** Remove AITestCase wrapper class ([b0bbd33](https://github.com/umbraco/Umbraco.AI/commit/b0bbd33350d1aa24aac17d25c507beed292ec915))
* **core,prompt,agent:** Use strong types and consistent JSON serializer options ([e26d786](https://github.com/umbraco/Umbraco.AI/commit/e26d786a023862bdfca899bf7bc31374c2b1f63c))
* **core:** Align test framework with Profile/Connection patterns ([f9d28b8](https://github.com/umbraco/Umbraco.AI/commit/f9d28b8f21250d6ab12040b1883aaa142ca655c3))
* **core:** Align test workspace with connections workspace patterns ([832436c](https://github.com/umbraco/Umbraco.AI/commit/832436c73cf4425851cf2829eff9dbac88cf8b7e))
* **core:** Complete rewrite of test workspace editor to follow Umbraco patterns ([53e1c84](https://github.com/umbraco/Umbraco.AI/commit/53e1c849341fc0893d5bb910ff5c1b7434d22449))
* **core:** Decouple AIEditableModelResolver from AIProviderCollection ([17c9a40](https://github.com/umbraco/Umbraco.AI/commit/17c9a40cbb3742162b88958eeabe5af48bb9393b))
* **core:** Migrate test workspace to collection pattern ([6f590a4](https://github.com/umbraco/Umbraco.AI/commit/6f590a4e69cec725a1f4811f5a23475e814b65b8))
* **core:** Move graders to new scoring view ([6487000](https://github.com/umbraco/Umbraco.AI/commit/64870006b53a0696460a9e07e588c43afe1f8f55))
* **core:** Move picker to subdirectory for better organization ([b4a5bf0](https://github.com/umbraco/Umbraco.AI/commit/b4a5bf0f56220ac834153ad631f606a0804663ef))
* **core:** Move test tag management to info view ([eab419f](https://github.com/umbraco/Umbraco.AI/commit/eab419f18d9ed799392120e2156018f31357ad0e))
* **core:** Remove ContextIds from feature config base ([f7df663](https://github.com/umbraco/Umbraco.AI/commit/f7df6638fb9b475f71f62bb6027dc7a5c03ffb6e))
* **core:** Remove final unused repository methods (YAGNI cleanup) ([6ab6355](https://github.com/umbraco/Umbraco.AI/commit/6ab63555f2ad669e63577a0094374576934ed587))
* **core:** Remove manual createScaffold - use base class implementation ([2271461](https://github.com/umbraco/Umbraco.AI/commit/227146144731c80e2d85fa6013f2d2b8829d479c))
* **core:** Remove unused tag filtering methods and fix misleading API ([9ca7bb3](https://github.com/umbraco/Umbraco.AI/commit/9ca7bb35eb3fdaf14d036c2f2b7fa404bdf7c5e7))
* **core:** Rename AITestGrader to AITestGraderConfig ([2104192](https://github.com/umbraco/Umbraco.AI/commit/210419290e951077b2ceb66d50b0ef3f97feb8b4))
* **core:** Replace IAIEntityFormatter with IAIEntityAdapter ([ff10ab0](https://github.com/umbraco/Umbraco.AI/commit/ff10ab0177fa182a8790592845d0c9adc8068a80))
* **core:** Replace manual grader UI with reusable component ([cb81318](https://github.com/umbraco/Umbraco.AI/commit/cb81318689ffba83f843e0c9306045111d3197e3))
* **core:** Rewrite CMS mock entity editor with native property editors ([27769f0](https://github.com/umbraco/Umbraco.AI/commit/27769f014b298e845bc66a9d6f3a67f9d055b8ec))
* **core:** Standardize test terminology from IsEnabled to IsActive ([1b04185](https://github.com/umbraco/Umbraco.AI/commit/1b041858ecd741905899e4de0e5366b0a3900531))
* **core:** Update test collection create action to match connections pattern ([083553d](https://github.com/umbraco/Umbraco.AI/commit/083553d6b7fa87c656a2e366eab5c34139167ca2))
* **core:** Use full TestRun naming in service methods and parameters ([5b709d6](https://github.com/umbraco/Umbraco.AI/commit/5b709d631acba2e3bde4e28b928d32359975a4eb))
* **core:** Use hey-api client for grader API calls ([7d853cd](https://github.com/umbraco/Umbraco.AI/commit/7d853cde6a04cd07da49ec61e57ca4e386332f9f))
* **core:** Use private field for data source access in repository ([7d79c24](https://github.com/umbraco/Umbraco.AI/commit/7d79c24f37e7515c4bff3d7bad231fe4205b19d9))
* **core:** Use UmbracoMapper in UpdateTestController ([c349ee0](https://github.com/umbraco/Umbraco.AI/commit/c349ee0608c205fb47fc128c2a18cab98a3c276a))
* **frontend:** Decompose AITestRepository into specialized repositories ([68ebe4e](https://github.com/umbraco/Umbraco.AI/commit/68ebe4e1fd0eb637635557153b26038a84770f59))
* **frontend:** Delegate run button to entity action ([c6697a8](https://github.com/umbraco/Umbraco.AI/commit/c6697a8555f54d23beec08ff68691b05cb2252f4))
* **frontend:** Extract grader results into standalone components ([a2b0a67](https://github.com/umbraco/Umbraco.AI/commit/a2b0a674ba70e67218fa5d76eaeeb2e097688bcc))
* **frontend:** Extract test entity context into reusable component ([eef745e](https://github.com/umbraco/Umbraco.AI/commit/eef745eabe31417175f7d4618fe0afa6920d43ac))
* **frontend:** Flip execution summary table orientation ([de8b8f6](https://github.com/umbraco/Umbraco.AI/commit/de8b8f6559b47e9017aadab3a6d59741203e3620))
* **frontend:** Move baseline tag to modal header ([a813bbe](https://github.com/umbraco/Umbraco.AI/commit/a813bbe80192271afaa07b5eac54deec3431305a))
* **frontend:** Move test run components to components folder ([720ee3d](https://github.com/umbraco/Umbraco.AI/commit/720ee3d22c70e80f3f4b37b764ca514673fd414a))
* **frontend:** Remove debugger statements from test entity ([bba295a](https://github.com/umbraco/Umbraco.AI/commit/bba295a59ba03faa8818445021849f670443fe76))
* **frontend:** Rename variation components with test- prefix and ([87a9a8b](https://github.com/umbraco/Umbraco.AI/commit/87a9a8b589239362d67f5614432670af50059f70))
* **frontend:** Simplify test run table columns ([ec80ae3](https://github.com/umbraco/Umbraco.AI/commit/ec80ae3665980d67d04b5274c928114f3f0a2a59))
* **frontend:** Unify comparison summary into single panel ([15d7c72](https://github.com/umbraco/Umbraco.AI/commit/15d7c7202e8366ee780e0e344bea8a99f56103a9))
* **frontend:** Use trophy icon column for best variation ([a586eb9](https://github.com/umbraco/Umbraco.AI/commit/a586eb91af13bbf59cb5de94cc843837dd14ad80))
* **frontend:** Use umb-property-layout in grader config editor ([0c607c5](https://github.com/umbraco/Umbraco.AI/commit/0c607c52c960aa114413a80477bec761b9023fbf))
* **frontend:** Use uui-box for comparison summary and flag icon for baseline ([91bd0fd](https://github.com/umbraco/Umbraco.AI/commit/91bd0fd2559d7ecd3a8d3a3cfa1dfe5167fc9632))

## [1.4.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI@1.4.0...Umbraco.AI@1.4.1) (2026-03-04)

### fix

* **core:** Use simple section alias for endpoint permissions ([bc8ef0b](https://github.com/umbraco/Umbraco.AI/commit/bc8ef0b8a4d8e8dd764a06deb9e1430d8dee59f6))

## [1.4.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI@1.3.0...Umbraco.AI@1.4.0) (2026-03-04)

### feat

* **core:** Make capability client creation async ([5f14ce1](https://github.com/umbraco/Umbraco.AI/commit/5f14ce1893496aac6b314afd56ab0d23f7674cbd))

### fix

* **core:** Prevent ResolveModel from mutating typed settings objects ([7f83734](https://github.com/umbraco/Umbraco.AI/commit/7f83734d6bce74cec4024817fdbd6f12d1d3475a))
* **core:** Use direct DB access for admin group section assignment migration ([eb43f5d](https://github.com/umbraco/Umbraco.AI/commit/eb43f5d76de3e3b49a8154d89b1402dbdcce0a1c))
* **frontend:** Prevent tag text wrapping in context and profile pickers ([fa284e0](https://github.com/umbraco/Umbraco.AI/commit/fa284e0f8b99e4874de72fdfebb3c28d5fa49dd8))

## [1.3.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI@1.2.0...Umbraco.AI@1.3.0) (2026-03-02)

### feat

* **core,deploy:** Add Deploy support for AISettings ([5b16415](https://github.com/umbraco/Umbraco.AI/commit/5b164152cee40ca7a2485a737417148f562c3d5a))
* **core:** Add concrete service connectors and notification handlers for Deploy ([38013de](https://github.com/umbraco/Umbraco.AI/commit/38013dedc350d38f7646c4cfa094a2370253583c))
* **core:** Add migration to grant Admin group access to AI section ([53efdc2](https://github.com/umbraco/Umbraco.AI/commit/53efdc27ef227573cea7307a917dddc27223d2a9))
* **core:** Add profile deletion protection for default settings ([8f93e8d](https://github.com/umbraco/Umbraco.AI/commit/8f93e8dd276efbd074eca06c2900d1aaf8618695))
* **core:** Add Saving and Deleting notifications for AISettings ([f7c210e](https://github.com/umbraco/Umbraco.AI/commit/f7c210e76123e8e4b82cbd1d8b1fce0424877650))
* **core:** Add Umbraco.AI.Deploy project structure and base classes ([0135286](https://github.com/umbraco/Umbraco.AI/commit/0135286a967332c8f9e99df28c45267adc72f447))
* **frontend:** Add entity actions support to container menu items ([11615bc](https://github.com/umbraco/Umbraco.AI/commit/11615bc0db73f437c7979749ed0a84943e26987a))

### fix

* **core,prompt,agent:** Fix compilation errors in Deploy packages ([3efbbb2](https://github.com/umbraco/Umbraco.AI/commit/3efbbb2cd780624419f9e069e5996f10494bd843))
* **core:** Allow connection alias to be updated on save ([130bceb](https://github.com/umbraco/Umbraco.AI/commit/130bcebc9f1cc99d09834289c101a8077cad2121)), closes [#75](https://github.com/umbraco/Umbraco.AI/issues/75)
* **core:** Fix raw string literal interpolation in LLMJudgeGrader ([20ca003](https://github.com/umbraco/Umbraco.AI/commit/20ca003464a614443c1e30059a4ba5fae32d2c68))
* **core:** Handle connection in-use check via AIConnectionDeletingNotification handler ([39dc1e9](https://github.com/umbraco/Umbraco.AI/commit/39dc1e914e3f2c05950e027e61ef10daaf4738f7)), closes [#76](https://github.com/umbraco/Umbraco.AI/issues/76)
* **core:** Prevent picker components from re-fetching on unchanged value ([07bd268](https://github.com/umbraco/Umbraco.AI/commit/07bd26888df6232646abcaac5d4ce6c4bfacca33)), closes [#70](https://github.com/umbraco/Umbraco.AI/issues/70)
* **core:** Reduce analytics aggregation log noise ([453fb42](https://github.com/umbraco/Umbraco.AI/commit/453fb42aca44282fff2d2f7f8af951510de3ea3c)), closes [#68](https://github.com/umbraco/Umbraco.AI/issues/68)
* **core:** Return ProblemDetails response when profile deletion is cancelled ([5e0a695](https://github.com/umbraco/Umbraco.AI/commit/5e0a695da48f8f6aa0bbe99d78fad5307cebe648))
* **core:** Update notification handler to use Umbraco.AI notification pattern ([fda4fd5](https://github.com/umbraco/Umbraco.AI/commit/fda4fd516a4807474ded2717752c728603675372))
* **frontend:** Show error toast notifications on delete failures ([ef6073d](https://github.com/umbraco/Umbraco.AI/commit/ef6073d389e13c6b7ded14b2dd3ce117353a904d))

### refactor

* **core,deploy:** Introduce IAIEntity interface to reduce boilerplate ([7b08de1](https://github.com/umbraco/Umbraco.AI/commit/7b08de15fb3faca38a2137902f6b2b2e263fc987))
* **core,deploy:** Remove deletion notifications for AISettings ([674c893](https://github.com/umbraco/Umbraco.AI/commit/674c89385b2079f9e564f7cc3f05bf579d5b2cdf))
* **core:** Rename ByConnection to WithConnection ([bf64267](https://github.com/umbraco/Umbraco.AI/commit/bf64267c052d10cc44afd38ad3b10e0e7b405a0a))

## [1.2.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI@1.1.0...Umbraco.AI@1.2.0) (2026-02-17)

### ⚠ Breaking change

* **core,agent:** from previous approach: Now distinguishes between context-bound
and cross-context tools based on ForEntityTypes declaration.

**Context-bound tools (filtered by entity type):**
- Backend tools with ForEntityTypes declared (e.g., content-read, media-write)
- Frontend tools with scopes that declare ForEntityTypes
- Only sent to LLM when editing matching entity types

**Cross-context tools (always available):**
- Backend tools without ForEntityTypes (e.g., search, navigation)
- Frontend tools without scopes or with scopes lacking ForEntityTypes
- Always sent to LLM regardless of current context

**Rationale:**
User could be editing a document but ask "show me media" - media tools should
be available. But frontend tools operating on current entity (e.g., set property)
should only appear when editing compatible entity types.

**LLM Context Awareness:**
Runtime context is already injected via AIRuntimeContextInjectingChatMiddleware,
providing entity type, section, user info. LLM uses this to make informed tool
choices without hard filtering cross-context tools.

Updated:
- AIToolContextFilter: Only filters by entity type (removed section filtering)
- AIAgentFactory: Added FilterFrontendToolsByContext for frontend tools
- Comments clarify context-bound vs cross-context distinction

### ⚠ BREAKING CHANGE

* **frontend:** UaiSerializedEntity structure changed for entity adapters

Changes:
- UaiSerializedEntity now has data field instead of contentType and properties
- Mark UaiSerializedProperty as deprecated (kept for reference)
- Update document adapter to nest structure inside data field:
  Before: { entityType, contentType, properties[] }
  After: { entityType, data: { contentType, properties[] } }

Allows third-party adapters to use domain-appropriate JSON structures
instead of being forced into rigid property arrays.
* **core:** AISerializedEntity.Properties replaced with Data field

Changes:
- AISerializedEntity now uses JsonElement Data instead of Properties collection
- Remove ContentType from top level (moved to data field)
- Mark AISerializedProperty as obsolete (kept for reference)
- Refactor AIEntityContextHelper to use formatter collection
- Update SerializedEntityContributor to check for data field
- BuildContextDictionary extracts data from JsonElement structure
- FormatForLlm delegates to entity-type-specific formatters

Adapters must now nest data inside the data field:
Before: { entityType, contentType, properties[] }
After: { entityType, data: { contentType, properties[] } }
* **core,agent:** Backend tools no longer filtered by context

**Section Context:**
- SectionContextContributor now adds system message to inform LLM
- Format: "## Current Section\nThe user is currently in the 'content' section."

**Tool Metadata:**
- AIFunctionFactory enriches tool descriptions with ForEntityTypes metadata
- Example: "Retrieves content documents. [Suitable for entity types: document, documentType]"
- Helps LLM choose appropriate tools based on entity type

**Backend Tool Filtering Removed:**
- Backend tools are NO LONGER filtered by context (cross-context by design)
- LLM uses runtime context (section, entity) + tool metadata to make informed decisions
- Frontend tools STILL filtered (operate on currently open entity)

**How it works:**
1. Runtime context injects section + entity info via system messages
2. Tool descriptions include ForEntityTypes metadata
3. Backend tools always available (LLM decides based on context + metadata)
4. Frontend tools filtered to current entity type only


* hide/show model property depending on loadingModels ([1c68091](https://github.com/umbraco/Umbraco.AI/commit/1c68091778d63a68888c667ec7c4b1ef6e2a232c))
* removing wrapper class from div ([1cf1ece](https://github.com/umbraco/Umbraco.AI/commit/1cf1ece58f3e3974a70f6cd75d5b836a70dfb06d))
* duplicated validation messages fix for model property ([ac0245e](https://github.com/umbraco/Umbraco.AI/commit/ac0245e9ffdf9ac8a340b441df399e5923f791fd))

### feat

* **agent,core:** Register surface context contributor at startup level ([ef7a576](https://github.com/umbraco/Umbraco.AI/commit/ef7a5769f465d479a0de241f29a431a14b8aefc4))
* **core,agent:** Add runtime context-based tool filtering to agent factory ([8f8c5ef](https://github.com/umbraco/Umbraco.AI/commit/8f8c5ef0bb7cd925f4ae9202e890771323296138))
* **core,agent:** Add section context and tool metadata to LLM ([c56e727](https://github.com/umbraco/Umbraco.AI/commit/c56e727fa0113540ef32c9cdab5c591f548605b2))
* **core,copilot:** Add manifest-driven request context contributor system ([ec3f985](https://github.com/umbraco/Umbraco.AI/commit/ec3f98503ed98b96e86f3a6c78f9320922eccb3b))
* **core,frontend:** Add Group property to AIField for visual field grouping ([16ec4bc](https://github.com/umbraco/Umbraco.AI/commit/16ec4bc503a2d5913eadb40b185ef4d597c11acb))
* **core,frontend:** Make model editor default group configurable ([ba16b58](https://github.com/umbraco/Umbraco.AI/commit/ba16b58170883e8e1848386e81cb9bd7e494f73d))
* **core,prompt,agent:** Add persistent menu highlighting for entity containers ([dd9cf3a](https://github.com/umbraco/Umbraco.AI/commit/dd9cf3a6aac91278a8003befc5c7719e4c0e7fd9))
* **core:** Add "Select All" checkbox to tool scope permissions ([0488e63](https://github.com/umbraco/Umbraco.AI/commit/0488e630c6192d0b31cb8adf8fa6db4744337f21))
* **core:** Add AIConnection and AIContext lifecycle notifications ([00176af](https://github.com/umbraco/Umbraco.AI/commit/00176afb445c1feba5b73fbf52f8fe9d6eda39ad))
* **core:** Add AIProfile lifecycle notifications ([7846fd6](https://github.com/umbraco/Umbraco.AI/commit/7846fd69f7253f73ccaf1ef6de1bfc721b5e8a10))
* **core:** Add base notification classes for entity lifecycle events ([e6fb38a](https://github.com/umbraco/Umbraco.AI/commit/e6fb38ad9465263f7c38d204d46de1fd0a80310a))
* **core:** Add entity formatter infrastructure ([3e3a09c](https://github.com/umbraco/Umbraco.AI/commit/3e3a09c5b60e276961c45843809e00a6df0236d5))
* **core:** Add ForEntityTypes to tool scopes for context filtering ([2cb9939](https://github.com/umbraco/Umbraco.AI/commit/2cb993961a97bf952532cabaa0afb77fad477a81))
* **core:** Add group labels for provider settings UI ([9809df5](https://github.com/umbraco/Umbraco.AI/commit/9809df51366c1bd642b6fb37c910306a8a0c4fbf))
* **core:** Add tool counts and empty scope filter to tool scope components ([222db17](https://github.com/umbraco/Umbraco.AI/commit/222db17a80e7af9bc343244a2fd527bf4c1063b8))
* **core:** Add welcome dashboard for AI section ([9a5318b](https://github.com/umbraco/Umbraco.AI/commit/9a5318b9ea3bf0960e86f6906823c97f3cca3bd6))
* **core:** Improve welcome dashboard styling and content ([92c91e2](https://github.com/umbraco/Umbraco.AI/commit/92c91e224fe8839e0ac4637b2e420ebce307fa65))
* **core:** Integrate notification publishing into AIConnection and AIContext services ([3202194](https://github.com/umbraco/Umbraco.AI/commit/3202194b5a5ca941b2d9a70ab98eeb97aea0a447))
* **core:** Integrate notification publishing into AIProfileService ([595deb8](https://github.com/umbraco/Umbraco.AI/commit/595deb81ab2fc891b4bd94a85d959f77d2a7fd4b))
* **core:** Integrate rollback notifications into Core services ([fd157e8](https://github.com/umbraco/Umbraco.AI/commit/fd157e8981c46e132046fcee2de204a82e3bb13e))
* **frontend:** Add tool count tags to scope picker modals ([c9680ef](https://github.com/umbraco/Umbraco.AI/commit/c9680ef5314667c824116e484c0fc98c9f2fef60))
* **frontend:** Localize tool count display using function-based localization ([f8fd5ae](https://github.com/umbraco/Umbraco.AI/commit/f8fd5aeb5621c42f86b746f7da625e70e13e3bda))

### fix

* **core,agent,prompt,copilot:** Add client ready promises to prevent race conditions ([8b961db](https://github.com/umbraco/Umbraco.AI/commit/8b961dbf5c0c8e74198772c6ff44e570238e7c1d))
* **core,agent,prompt:** Migrate authorization to custom AI section ([c5b4503](https://github.com/umbraco/Umbraco.AI/commit/c5b4503031fa5612f636409cc876659e4632fe82))
* **core,agent:** Only filter context-bound tools (those with ForEntityTypes) ([8322ad0](https://github.com/umbraco/Umbraco.AI/commit/8322ad0f3e27a93b91ca295c3cc774f0694fc252))
* **core,prompt,agent:** Use IEventAggregator instead of INotificationPublisher for Umbraco v17 ([0385280](https://github.com/umbraco/Umbraco.AI/commit/03852809bb696a4eaeaf8150f3e30deb6f9377bb))
* **core:** Add IEventAggregator mocks to service unit tests ([30544ec](https://github.com/umbraco/Umbraco.AI/commit/30544ec7f8a215ec51f62ba3a3fb17f01c3fdc16))
* **core:** Add missing IEventAggregator to integration test setup ([312f67f](https://github.com/umbraco/Umbraco.AI/commit/312f67f6b0c6f7ad81500b14928d6dad375ca5d0))
* **core:** Fix AIFunctionFactoryTests to work with sealed AIToolScopeCollection ([6a0aa50](https://github.com/umbraco/Umbraco.AI/commit/6a0aa50fc5d5e63f2fdb187af097c33b850ab08b))
* **core:** Fix compilation errors in entity adapter tests ([8386a24](https://github.com/umbraco/Umbraco.AI/commit/8386a245aced4d8dc6c8a0fb89eddc940e7d9741))
* **core:** Fix entity contributor race condition ([83a99a4](https://github.com/umbraco/Umbraco.AI/commit/83a99a407e79582b43f92a9160ce64432457e62b))
* **core:** Fix race condition causing empty tool scope list ([8585c72](https://github.com/umbraco/Umbraco.AI/commit/8585c72fc30451b21a238bb8ec822a117f1aa803))
* **core:** Fix StatefulNotification constructor and update tests with IEventAggregator ([97eeceb](https://github.com/umbraco/Umbraco.AI/commit/97eeceb1125b05b1fa6d64a631d136735d75121b))
* **core:** Fix timezone detection in timestamp formatter ([0734deb](https://github.com/umbraco/Umbraco.AI/commit/0734deb46b96b7f4778a287ba40f72655f355c96))
* **core:** Fix timezone handling in AI Logs timestamps ([e6bfc33](https://github.com/umbraco/Umbraco.AI/commit/e6bfc33e30cc8f58114a86722c21f846e165ff93)), closes [#49](https://github.com/umbraco/Umbraco.AI/issues/49)
* **core:** Handle JsonElement deserialization in typed tools ([9f2772b](https://github.com/umbraco/Umbraco.AI/commit/9f2772b2fb1fa4e46dd24c4d8bb4b056b7ec5977)), closes [#48](https://github.com/umbraco/Umbraco.AI/issues/48)
* **core:** Skip encryption for configuration references in sensitive fields ([b425567](https://github.com/umbraco/Umbraco.AI/commit/b425567a04146e54ff50c945aa514be6123d0f36))
* **core:** Use correct extension API for frontend tool repository lookup ([2903ac8](https://github.com/umbraco/Umbraco.AI/commit/2903ac852a9538f4317abc7b15e8a16a5b6a998d))
* **core:** Use relative import to avoid circular dependency in tool controller ([dfb03fd](https://github.com/umbraco/Umbraco.AI/commit/dfb03fd240570abf56a271941655759eed593784))

### refactor

* **core,agent-ui,copilot:** Extract surface contributor into reusable kind ([7e2100c](https://github.com/umbraco/Umbraco.AI/commit/7e2100c5c3a69f81b71988378fc6997f4beaa5c5))
* **core,agent-ui,copilot:** Rename surface kind to agentSurface ([c578ef3](https://github.com/umbraco/Umbraco.AI/commit/c578ef3aa9a88f925241b2ea811dc83563d19a5c))
* **core,agent,copilot:** Rename SectionAlias to Section ([353e167](https://github.com/umbraco/Umbraco.AI/commit/353e1670fdcdc09f88c9055d81d7b688b693bccc))
* **core,frontend:** Construct group localization keys by convention ([c1806d0](https://github.com/umbraco/Umbraco.AI/commit/c1806d0816e56903d1a9e754ef5921a5c62e9251))
* **core,frontend:** Move group localization key construction to frontend ([dc9afd5](https://github.com/umbraco/Umbraco.AI/commit/dc9afd57f8534c495776d42dff40b0ac5b74067d))
* **core,prompt:** Rename PropertyChange to ValueChange ([bcc58fd](https://github.com/umbraco/Umbraco.AI/commit/bcc58fdc44178839d78ec4daf7f1a52828827720))
* **core:** Improve version history pagination layout ([22b7272](https://github.com/umbraco/Umbraco.AI/commit/22b7272e3b19760f783f1b9638c9eb63c3e90a9a))
* **core:** Remove documentType and mediaType from built-in scopes ([f0c4f16](https://github.com/umbraco/Umbraco.AI/commit/f0c4f166662835a8186c66b4ede7f339b3d62240))
* **core:** Remove pagination wrapper div and styles ([2360b12](https://github.com/umbraco/Umbraco.AI/commit/2360b1214e2879bbff2b301970324284caf6a20c))
* **core:** Replace custom pagination with uui-pagination component ([814bdb9](https://github.com/umbraco/Umbraco.AI/commit/814bdb90579e4813af71e050a4d660d26be14812))
* **core:** Replace Properties with Data field in AISerializedEntity ([d3edbbb](https://github.com/umbraco/Umbraco.AI/commit/d3edbbbf5a501b491628afb06b7f709a8ff6eb0c))
* **core:** Simplify field group localization key generation ([4f75f27](https://github.com/umbraco/Umbraco.AI/commit/4f75f27eda7871d18720f058705b2d2dc818041c))
* **core:** Update version diff column header from Alias to Name ([2a0eee0](https://github.com/umbraco/Umbraco.AI/commit/2a0eee0853c70cb1791b3d7a77fa275f060f3ffb))
* **core:** Use C# property initializers for field default values ([1687d95](https://github.com/umbraco/Umbraco.AI/commit/1687d9566416ec377cf45af5c3bc6c068b531621))
* **frontend:** Simplify model editor to always group fields ([4ae056e](https://github.com/umbraco/Umbraco.AI/commit/4ae056e6daf5d2b6907458838c8f77418a31c591))
* **frontend:** Update entity adapter types to use data field ([ae1fbdb](https://github.com/umbraco/Umbraco.AI/commit/ae1fbdbd0666ec207a85b5fb90596c8018bb4809))

## [1.1.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI@1.0.0...Umbraco.AI@1.1.0) (2026-02-10)

### ⚠ BREAKING CHANGE

* **core:** Removes the DetailLevel setting from audit logs and changes
default persistence behavior.

- Remove AIAuditLogDetailLevel enum (was never checked in code)
- Remove DetailLevel from AIAuditLog model and AIAuditLogEntity
- Remove DetailLevel from API response models
- Change PersistPrompts default from false to true
- Change PersistResponses default from false to true
- Keep PersistFailureDetails default as true
- Create database migrations to drop DetailLevel column (SQL Server & SQLite)
- Update docs to reference PersistPrompts/PersistResponses

The three boolean flags provide clearer, more flexible control than abstract
detail levels. Users can still disable any persistence option in
appsettings.json if needed for privacy.


### feat

* add missing properties to uaiProfile ([701c738](https://github.com/umbraco/Umbraco.AI/commit/701c7383cc26e1fdc7c2d81c7bb17d0797435b54))
* **core,agent,copilot:** Add tool metadata flow for scope and destructive permissions ([6e9efbc](https://github.com/umbraco/Umbraco.AI/commit/6e9efbc5cd01b4fb79ec3731a47e4ca64b05efb7))
* **core,agent,prompt:** Add alias existence API endpoints ([f48d7e9](https://github.com/umbraco/Umbraco.AI/commit/f48d7e9eb8218fee27adbb8bec00ed74c0c897ff))
* **core,agent,prompt:** Add visual error indicators for duplicate alias validation ([cdc56f0](https://github.com/umbraco/Umbraco.AI/commit/cdc56f0ea9f394a6acbbe7e8f43cd12bfbee4a65))
* **core:** Add ConnectionAliasExistsAsync and ProfileAliasExistsAsync methods ([b1d1f95](https://github.com/umbraco/Umbraco.AI/commit/b1d1f95fc835ead5cb69d2f6181c143183eabf7e))
* **core:** Add context alias uniqueness validation backend ([76e85ff](https://github.com/umbraco/Umbraco.AI/commit/76e85ffe8bc9df86a37d5aac67f89955a396c2e3))
* **core:** Add generic user-group-settings-list component ([6978289](https://github.com/umbraco/Umbraco.AI/commit/697828987fbdcabdd355ab19f699a4293d905db1))
* **core:** Add localization entries for built-in tools ([9c69a2a](https://github.com/umbraco/Umbraco.AI/commit/9c69a2a1a73592048bcbcc9ea2fcb4ba7717d0fd))
* **core:** Add localization for get_umbraco_media tool ([41d9647](https://github.com/umbraco/Umbraco.AI/commit/41d96478c4b1f13e4f76c8fa394d95e8f085791e))
* **core:** Add localization support for tool names in picker ([bfb1390](https://github.com/umbraco/Umbraco.AI/commit/bfb1390f6ef03b2f32a6787f183c4d96d9d355d5))
* **core:** Add tool scope API endpoint ([2c2f913](https://github.com/umbraco/Umbraco.AI/commit/2c2f9135a710c0f2ca1c0e6a7affe5c280f4da17))
* **core:** Add validation to Profile workspace ([3f9f3f4](https://github.com/umbraco/Umbraco.AI/commit/3f9f3f4719ddc9604d40da04ac9764c94d86a5fd))
* **core:** Display full scope/tool info in permission override components ([e014db6](https://github.com/umbraco/Umbraco.AI/commit/e014db66b9ae199c8e7a54f5a6f0661fb75436de))
* **core:** Make user group list items clickable to open editor ([0b06162](https://github.com/umbraco/Umbraco.AI/commit/0b06162cdb5a0a1d5f7491e8b03d00e28baa7ee6))
* **core:** Replace text input with tool picker modal in tool permissions override ([02d50ed](https://github.com/umbraco/Umbraco.AI/commit/02d50eda4812d46f94fef11a4b59e04bedb2090a))
* **core:** Update built-in tool scope icons ([8639ba7](https://github.com/umbraco/Umbraco.AI/commit/8639ba72850d7724bb5ebbc323ef9f8f10b8e89a))
* **frontend:** Add validation localization strings ([a5094f9](https://github.com/umbraco/Umbraco.AI/commit/a5094f9bf70064025f6967bf3d2592f74a229925))
* **frontend:** Implement Connection workspace validation ([a4c655d](https://github.com/umbraco/Umbraco.AI/commit/a4c655d3a3e3c3ecb54183b63ba0733fe96950aa))

### fix

* **core,agent,prompt:** Add checkValidity() to trigger visual validation state ([b1dc012](https://github.com/umbraco/Umbraco.AI/commit/b1dc0129791129e28becc7b8ff9165d72e628000))
* **core,agent,prompt:** Escape hyphen in regex pattern ([49092af](https://github.com/umbraco/Umbraco.AI/commit/49092afef2ce95239bbc33cd748df9d5fdb37fbd))
* **core,agent,prompt:** Fix validation blocking by calling checkValidity() method ([1766901](https://github.com/umbraco/Umbraco.AI/commit/1766901e5e308235aed721bdbfc90d84e3c07fd5))
* **core,agent,prompt:** Fix validation issues from code review ([5984276](https://github.com/umbraco/Umbraco.AI/commit/5984276e463b7dccd625edf1181c4c950b6555a7))
* **core,agent,prompt:** Properly block save on duplicate alias via validation messages ([908d1c8](https://github.com/umbraco/Umbraco.AI/commit/908d1c8592755e08a88e92ef0e246b0783479da5))
* **core,agent,prompt:** Use setCustomValidity for visual error display ([346f0e8](https://github.com/umbraco/Umbraco.AI/commit/346f0e8ad04776c0b39c67d39695ce00962c7520))
* **core,agent:** Fix breaking unit tests ([14f9c47](https://github.com/umbraco/Umbraco.AI/commit/14f9c4705fa50065501df3c4b88357db185d2858))
* **core,tools:** Fix TypeScript compilation errors ([feae61a](https://github.com/umbraco/Umbraco.AI/commit/feae61a279c8181e189c4206a29375309078cb7c))
* **core:** Actually write the entity key to the system prompt as context ([fb5cefd](https://github.com/umbraco/Umbraco.AI/commit/fb5cefd48c38ef9c0310d3cd62aaf0b7ac10d721))
* **core:** Add AISettingsEntity to migration designer files ([bc5cd43](https://github.com/umbraco/Umbraco.AI/commit/bc5cd4364ec701739e2266b5f521f75ef77178b0))
* **core:** Add missing localization keys for workspace labels ([77c3722](https://github.com/umbraco/Umbraco.AI/commit/77c37228fc36a87fa364b60b17e8cd9e32773337)), closes [#2](https://github.com/umbraco/Umbraco.AI/issues/2)
* **core:** Add null check for userGroupId from picker ([138ac4e](https://github.com/umbraco/Umbraco.AI/commit/138ac4ee0445943c3a720b0cda2dfe1bd89d4550))
* **core:** Fixed core package being too liberal with the umbraco-marketplace tag ([5c660a0](https://github.com/umbraco/Umbraco.AI/commit/5c660a07e6352df18265eac43f52bf47ea660cb9))
* **core:** Include AISettings table in GUID user ID migration ([96c76fa](https://github.com/umbraco/Umbraco.AI/commit/96c76fa2e867ca4f7b519f50afaba06daabe7942)), closes [#44](https://github.com/umbraco/Umbraco.AI/issues/44)
* **core:** Instantiate UmbUserGroupItemRepository directly ([90486e5](https://github.com/umbraco/Umbraco.AI/commit/90486e511dfd66c81d001382a884b6d113af36c7))
* **core:** Make ToolScopeMapDefinition public for auto-discovery ([a14e2ce](https://github.com/umbraco/Umbraco.AI/commit/a14e2cefbeed23bab1e9ad76334f30c3a51e0314))
* **core:** Register ToolScopeMapDefinition with DI container ([7d4b114](https://github.com/umbraco/Umbraco.AI/commit/7d4b11405c0088599b7e79a00092aadd4d3f6d85))
* **core:** Reload tool picker items when frontendTools property changes ([7babd11](https://github.com/umbraco/Umbraco.AI/commit/7babd114e5790991599278f7e83f1e543854fffc))
* **core:** Use relative imports in tool-scope-permissions component ([47bf187](https://github.com/umbraco/Umbraco.AI/commit/47bf187618469296ba79a2bffc8be5cd37f9ba65))
* **core:** Use user group repository instead of direct API access ([e8a2998](https://github.com/umbraco/Umbraco.AI/commit/e8a2998080e39fff85c8ff877800a8f693d4a95c))
* **frontend:** Fix TypeScript compilation errors for workspace validation ([277abc6](https://github.com/umbraco/Umbraco.AI/commit/277abc61b519655bd3c989d35e5850011d5e265f))
* **frontend:** Validate auto-generated aliases on name change ([35dbe84](https://github.com/umbraco/Umbraco.AI/commit/35dbe848759643e2176e241ff6bf05e8455d8573))

### refactor

* **core:** Extract toCamelCase to shared utility ([4ebdc74](https://github.com/umbraco/Umbraco.AI/commit/4ebdc740bf831ff8031db0b7d81c9c834e786836))
* **core:** Move tool scope permissions component to Core ([e084fab](https://github.com/umbraco/Umbraco.AI/commit/e084fab20d1d64a39fab1eb9041cff7f8d093bd6))
* **core:** Remove unused DetailLevel setting and set PersistX defaults to true ([d9c8a75](https://github.com/umbraco/Umbraco.AI/commit/d9c8a757756a52d4e07a330ef143af239999e74a))
* **core:** Update tool scope permissions export path ([644967f](https://github.com/umbraco/Umbraco.AI/commit/644967f8ed88b6d5b19748bfddba0b66bd1947bc))

## [1.0.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI@1.0.0...Umbraco.AI@1.0.1) (2026-02-04)

### ⚠ BREAKING CHANGE

* **core:** Removes the DetailLevel setting from audit logs and changes
default persistence behavior.

- Remove AIAuditLogDetailLevel enum (was never checked in code)
- Remove DetailLevel from AIAuditLog model and AIAuditLogEntity
- Remove DetailLevel from API response models
- Change PersistPrompts default from false to true
- Change PersistResponses default from false to true
- Keep PersistFailureDetails default as true
- Create database migrations to drop DetailLevel column (SQL Server & SQLite)
- Update docs to reference PersistPrompts/PersistResponses

The three boolean flags provide clearer, more flexible control than abstract
detail levels. Users can still disable any persistence option in
appsettings.json if needed for privacy.

### feat

* add missing properties to uaiProfile ([701c738](https://github.com/umbraco/Umbraco.AI/commit/701c7383cc26e1fdc7c2d81c7bb17d0797435b54))

### fix

* **core:** Fixed core package being too liberal with the umbraco-marketplace tag ([5c660a0](https://github.com/umbraco/Umbraco.AI/commit/5c660a07e6352df18265eac43f52bf47ea660cb9))

### refactor

* **core:** Remove unused DetailLevel setting and set PersistX defaults to true ([d9c8a75](https://github.com/umbraco/Umbraco.AI/commit/d9c8a757756a52d4e07a330ef143af239999e74a))

## [1.0.0] - 2026-02-03

Initial release.

[1.0.0]: https://github.com/umbraco/Umbraco.AI/releases/tag/Umbraco.AI@1.0.0
