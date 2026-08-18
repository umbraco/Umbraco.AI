# Changelog - Umbraco.AI.Agent.Copilot.Workspace

All notable changes to Umbraco.AI.Agent.Copilot.Workspace will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).


## [18.0.0-rc.1](https://github.com/umbraco/Umbraco.AI/releases/tag/Umbraco.AI.Agent.Copilot.Workspace@18.0.0-rc.1) (2026-08-18)

### feat

* **agent-ui,copilot-workspace:** Add an archived-conversations recycle bin with read-only viewing ([af0e365](https://github.com/umbraco/Umbraco.AI/commit/af0e3659c67eda29f997049008c708a0fb4bd99c))
* **agent-ui,copilot-workspace:** Move chat scrollbar to the edge, keep centered content ([cd49784](https://github.com/umbraco/Umbraco.AI/commit/cd4978406da4b070ea02586b4a519b025d71b7c4))
* **agent,conversations:** Rehydrate HITL approvals from persisted history (B2) ([e741206](https://github.com/umbraco/Umbraco.AI/commit/e7412067d7b49155288414b31cdd04327cc23cca))
* **conversations,copilot-workspace:** Add conversation CRUD API + OpenAPI doc ([e7398a9](https://github.com/umbraco/Umbraco.AI/commit/e7398a928fba949c3ae032c5ebcae1b8163aa31e))
* **conversations:** Add conversation/message domain models, repository, and factories ([8d8a587](https://github.com/umbraco/Umbraco.AI/commit/8d8a58793b23735112ba8d31902f6a9a9af63c2f))
* **conversations:** Add ConversationChatHistoryProvider (MAF custom-storage bridge) ([10e45fa](https://github.com/umbraco/Umbraco.AI/commit/10e45fac01803d30cfcbcb384fc3059a7171e705))
* **conversations:** Add entity lifecycle notifications and block in-use project deletes ([ed60360](https://github.com/umbraco/Umbraco.AI/commit/ed60360e24a32da5a3fe9962e1d22cc71c7792ff))
* **conversations:** Add ownership-enforcing conversation/project services ([66780d8](https://github.com/umbraco/Umbraco.AI/commit/66780d880e9aaa36840b8cd0d739e33a4a383d27))
* **conversations:** Add persistence schema, DbContext, and initial migrations ([36f88ee](https://github.com/umbraco/Umbraco.AI/commit/36f88ee98fb91034175f51e873cb38dab2337ab6))
* **conversations:** Add project CRUD API ([67783b2](https://github.com/umbraco/Umbraco.AI/commit/67783b29500ff3588325449d4ba667a59578b0ba))
* **conversations:** Add project repository with resource-settings serialization ([c52743f](https://github.com/umbraco/Umbraco.AI/commit/c52743fd60fd2e10321df0331384e4835697eaa7))
* **copilot-workspace, agent-ui:** Conversation polish — move to project, auto-title, focus ([7e003c5](https://github.com/umbraco/Umbraco.AI/commit/7e003c5a6fcac5c74b6d72dfa2ca39ed624646bc))
* **copilot-workspace:** Add authenticated ownership-checked file endpoint (B6) ([3b8995e](https://github.com/umbraco/Umbraco.AI/commit/3b8995e83644fbf03c6bf1117a382f4187220b38))
* **copilot-workspace:** Add conversation-scoped context & resources ([5b8e2ad](https://github.com/umbraco/Umbraco.AI/commit/5b8e2adfc34ae958bf284522b4c08d57cad1028c))
* **copilot-workspace:** Add Copilot Workspace section-access authorization policy ([eca6309](https://github.com/umbraco/Umbraco.AI/commit/eca630959dccd578316b68572bd56b2915bd63a2))
* **copilot-workspace:** Add New project to the top-level create menu too ([331935e](https://github.com/umbraco/Umbraco.AI/commit/331935e468d482fb12550b1936d5cf20ebfc1d1c))
* **copilot-workspace:** Add persisted AG-UI stream endpoint (A) ([ef02f1e](https://github.com/umbraco/Umbraco.AI/commit/ef02f1eaccc101c528d00f519cd6346de6e1d85e))
* **copilot-workspace:** Add the projects UI (create, edit, delete) ([166a09f](https://github.com/umbraco/Umbraco.AI/commit/166a09f6cb7db3bfd2a137d0642e4c2b7a6af154))
* **copilot-workspace:** Add three-region section shell with routed center (Phase 5) ([848245a](https://github.com/umbraco/Umbraco.AI/commit/848245a2615c696877e9e760f720b9eba28c79f5))
* **copilot-workspace:** Adopt CMS primitives for the sidebar tree ([a5fd036](https://github.com/umbraco/Umbraco.AI/commit/a5fd036efd617413dbe44df2fcfb052dc8e007de))
* **copilot-workspace:** Align the project editor to a standard workspace; add a launcher ([d185c9e](https://github.com/umbraco/Umbraco.AI/commit/d185c9e92d48a6b755814c65a854ffb3e728c2af))
* **copilot-workspace:** Auto-assign Workspace section to Admin group ([03b7b50](https://github.com/umbraco/Umbraco.AI/commit/03b7b5085a1273c871244aaffc763fc2ffe36981))
* **copilot-workspace:** Centered create-style project picker for New chat in a project ([0864c96](https://github.com/umbraco/Umbraco.AI/commit/0864c9693dcd65b32d335ac84ba3c3fc0c8d765c))
* **copilot-workspace:** Constrain chat to a centered comfortable width ([2daa204](https://github.com/umbraco/Umbraco.AI/commit/2daa20408de70ef24fc3c5dd757bdd612881ff1e))
* **copilot-workspace:** Convert sidebar to a projects tree with entity-action menus ([e556dfb](https://github.com/umbraco/Umbraco.AI/commit/e556dfbf048d9b184d8cd8cd3b769f622a5ec15c))
* **copilot-workspace:** Data-bind the conversation-list sidebar over the management API ([b65130c](https://github.com/umbraco/Umbraco.AI/commit/b65130c977938b39ccd6c5e5246d8c5dbd7e0e47))
* **copilot-workspace:** Inject project instructions, framing and resources into chats ([f5b1d12](https://github.com/umbraco/Umbraco.AI/commit/f5b1d121f0503cf1b0a9990fa854b5da45f5b327))
* **copilot-workspace:** Let an unsaved chat hold its own contexts and resources ([bf91832](https://github.com/umbraco/Umbraco.AI/commit/bf9183278eb03c70bb876b928c871356f27c7aef))
* **copilot-workspace:** Make projects a reactive store; show empty projects in the tree ([be4e679](https://github.com/umbraco/Umbraco.AI/commit/be4e679efd90a20616e74156a31e9b650a250b29))
* **copilot-workspace:** Make the context panel collapsible + resizable ([a64f336](https://github.com/umbraco/Umbraco.AI/commit/a64f33628c6e057dcbee8308c5e306fc366bc684))
* **copilot-workspace:** New chat split button (New chat / New chat in a project) ([7d6bc85](https://github.com/umbraco/Umbraco.AI/commit/7d6bc856bf3830656f9ed557320ce0cd34fcd444))
* **copilot-workspace:** Persist a conversation only on its first message ([7b29721](https://github.com/umbraco/Umbraco.AI/commit/7b29721b46b44e531af97f5c8ffcf6c7db042303))
* **copilot-workspace:** Polish context sidebar ([6a64de6](https://github.com/umbraco/Umbraco.AI/commit/6a64de6cd90ca332c8b6103211ffea95da01a7ee))
* **copilot-workspace:** Populate the context panel with the conversation's project context ([65cd8e2](https://github.com/umbraco/Umbraco.AI/commit/65cd8e2f82a1743442d95f273c1e3935eb5b757c))
* **copilot-workspace:** Register sidebar groups as sectionSidebarApps ([d73dd11](https://github.com/umbraco/Umbraco.AI/commit/d73dd1148bb2fb4b17927b74303f3c5615d8ff3b))
* **copilot-workspace:** Scaffold Copilot Workspace product and Conversations backend ([db6e49f](https://github.com/umbraco/Umbraco.AI/commit/db6e49f690c7acf067dcc095fe6d1630c79ee25e)), closes [#2](https://github.com/umbraco/Umbraco.AI/issues/2)
* **copilot-workspace:** Scaffold frontend + register the Workspace section (Phase 5) ([9a4dcd6](https://github.com/umbraco/Umbraco.AI/commit/9a4dcd63030028c0eebad44f3f95ff128ac6897b))
* **copilot-workspace:** Search-filter projects; show project on launcher recents ([675a942](https://github.com/umbraco/Umbraco.AI/commit/675a9426556f1355ebc6c939c484b7cf0f08a2e5))
* **copilot-workspace:** Wire the center chat over the shared Agent.UI stack ([0ecbc4a](https://github.com/umbraco/Umbraco.AI/commit/0ecbc4a3c341d818ce229e5e83dea60c9acafd78))
* **core,copilot-workspace:** Mark readonly picker rows with a no-entry hint ([7384a8f](https://github.com/umbraco/Umbraco.AI/commit/7384a8f6a4ffa892de2169d54b211d62f5f3a100))
* **core,copilot-workspace:** Show project and conversation context as one list ([f5f0480](https://github.com/umbraco/Umbraco.AI/commit/f5f0480a354e1fb3d5befa894b8c92b538838c92))

### fix

* **agent,conversations:** Lead every agent request with the runtime-context system message ([544459d](https://github.com/umbraco/Umbraco.AI/commit/544459da5224805595769ecb2c0683f22adc07a6))
* **agent,copilot-workspace:** Stop double-storing attachment bytes in conversation history ([3c6390a](https://github.com/umbraco/Umbraco.AI/commit/3c6390a39e7b7d1ab3de526c327d60ab01e247af))
* **agent,copilot-workspace:** Stop the file retention sweep aging out live persisted conversations ([3be517c](https://github.com/umbraco/Umbraco.AI/commit/3be517c039369084a241466b5a5f1c69786641b9))
* **conversations,agent-ui:** Replace the regenerated answer instead of appending it ([f810316](https://github.com/umbraco/Umbraco.AI/commit/f8103168cb42ce6a5c0417ca9f999ff97ba09d75))
* **copilot-workspace:** Auto-select an agent when the catalog loads after the conversation opens ([3375bc2](https://github.com/umbraco/Umbraco.AI/commit/3375bc24da1d3e9eb11eab6bab78472ada166f77))
* **copilot-workspace:** Keep chat icon on launcher recents ([1c332d1](https://github.com/umbraco/Umbraco.AI/commit/1c332d17309abce252bcd4bd345957285713d61a))
* **copilot-workspace:** Keep New project in the top menu only; indent empty hint ([6acabcc](https://github.com/umbraco/Umbraco.AI/commit/6acabcceff30a9ace9f1be5684f158ba706cc806))
* **copilot-workspace:** Localize the copilot-workspace agent-surface label ([e8011b8](https://github.com/umbraco/Umbraco.AI/commit/e8011b87818806a59883572bb6f65369997ec5c0))
* **copilot-workspace:** Match the new-chat route to the path the buttons generate ([1810768](https://github.com/umbraco/Umbraco.AI/commit/18107681e30bef7b5a2cef6f548f8f6e5e524c1c))
* **copilot-workspace:** Move New chat popover out of the button group ([3bc66d9](https://github.com/umbraco/Umbraco.AI/commit/3bc66d9822c5761d2245e301f5b270f045d89705))
* **copilot-workspace:** Remove the redundant conversation file endpoint ([1bc9dbc](https://github.com/umbraco/Umbraco.AI/commit/1bc9dbc7723e4efc07fc149873e8e36c97b6cbed))
* **copilot-workspace:** Render project nodes as uui-menu-item for consistency ([151d3e4](https://github.com/umbraco/Umbraco.AI/commit/151d3e40fc7585d61b3447787b61e438d651a64f))
* **copilot-workspace:** Render the workspace as a standalone section element, not a dashboard ([6f7f3c7](https://github.com/umbraco/Umbraco.AI/commit/6f7f3c7b4d301e74f8ef624db90c5156ad727a3b))
* **copilot-workspace:** Shorten section label to "Copilot" ([7c7161c](https://github.com/umbraco/Umbraco.AI/commit/7c7161cf753c89b5b0dedc0eaf916032465639d9))
* **copilot-workspace:** Size New chat caret to content; make sidebar header sticky ([6d98df2](https://github.com/umbraco/Umbraco.AI/commit/6d98df2b9e170d005ed26453c9086cdca03ccaee))
* **copilot-workspace:** Use + icon for the New project group action ([557ed67](https://github.com/umbraco/Umbraco.AI/commit/557ed671841caf599e3fe50190b73131f43883b0))
* **copilot-workspace:** Use icon-pushpin for the pin icon ([8f0ad19](https://github.com/umbraco/Umbraco.AI/commit/8f0ad195acdad6478ad8ba9fdfb537854e9aff35))
* **copilot-workspace:** Wrap New-chat project picker rows in a ref-list ([11681d6](https://github.com/umbraco/Umbraco.AI/commit/11681d6178fe5c4fd7980ffa6c3c54b0d0362844))
* **core,agent,agent-ui,copilot,copilot-workspace,ci:** Stop leaking internal components into the shared entry file (#327) ([990a987](https://github.com/umbraco/Umbraco.AI/commit/990a9879fec349f8d2f2b337bb581fbe5272c45c)), closes [#327](https://github.com/umbraco/Umbraco.AI/issues/327) [#324](https://github.com/umbraco/Umbraco.AI/issues/324) [#324](https://github.com/umbraco/Umbraco.AI/issues/324)
* **core,copilot-workspace:** Stop the workspace store refetching its own write ([c35c82e](https://github.com/umbraco/Umbraco.AI/commit/c35c82ede17017d0574e0ceee02ac06440ae3549))

### refactor

* **copilot-workspace:** Make Conversations web layer host-agnostic ([000307b](https://github.com/umbraco/Umbraco.AI/commit/000307baaad14974775eb3710877d8da2db01d46))
* **copilot-workspace:** Make the open conversation a reactive workspace store ([c43ed6d](https://github.com/umbraco/Umbraco.AI/commit/c43ed6d3f10f61d6039bc3d42a7f97b387831211))
* **copilot-workspace:** Move conversation list into the CMS section sidebar ([e569c82](https://github.com/umbraco/Umbraco.AI/commit/e569c8292663f063da5024fbd54c5f6765763285))
* **copilot-workspace:** Rename section alias to Uai.Section.CopilotWorkspace ([e9d8124](https://github.com/umbraco/Umbraco.AI/commit/e9d81247885d0dab117dadc67e9bdd65b76a897e))
* **copilot-workspace:** Reorganize front-end topic-first and de-duplicate ([237beb0](https://github.com/umbraco/Umbraco.AI/commit/237beb03939b44c1c61cb46613d416a4d9bd1f15))
* **core,agent,copilot-workspace:** Render entity pickers as flat rows ([601fadb](https://github.com/umbraco/Umbraco.AI/commit/601fadb0b33aeb9c27fbde4b774bd000e0e4f5a6))