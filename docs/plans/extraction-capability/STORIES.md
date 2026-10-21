# Stories

Derived from `SPEC.md` and `ARCHITECTURE.md`. Story ids (`EX-n`) are referenced by `PLAN.md` and the
spec files, so never renumber them.

## Definition of Ready / Definition of Done

> ASSUMPTION: Same agreement the shipped Decision feature used. Correct it here if the team wants
> something different.

**Ready:** role, capability and value are stated and not hollow. Given/When/Then criteria cover the
happy path. Out of scope is explicit. Passes INVEST.

**Done:**

- Every criterion passes as an executable spec, sad paths included.
- The whole surface is inert while `Umbraco:AI:Experimental:Extraction` is off (the default).
- Every real entry point (endpoint, composer, manifest, Automate action) is checked on the demo
  site, not just by unit tests.
- Shipped on `v18/dev` and ported to `v17/dev`.

---

## EX-1: Extract typed values from C#

As a **package or site developer**,
I want to pass some text and a list of typed fields to `IAIExtractionService`,
so that I get typed values back without prompting a chat model and parsing its reply.

**Happy path**

- AC1: text field returns a string and its position
  Given an Extraction profile and a provider that returns `"A-88412"` at characters 6 to 13
  When I call `ExtractAsync(new AITextExtractionField { Instructions = "The order number" }, state)`
  Then `Answer` is an `AITextExtractionAnswer` with `Value = "A-88412"` and `Span = (6, 13)`
- AC2: number field returns a decimal
  Given a provider that returns `701.10` for a number field
  When I extract it
  Then `Answer.Value` is the decimal `701.10`
- AC3: date field returns a DateOnly
  Given a provider that returns `"2026-10-03"` for a date field
  When I extract it
  Then `Answer.Value` is `new DateOnly(2026, 10, 3)`
- AC4: time field returns a TimeOnly
  Given a provider that returns `"14:30"` for a time field
  When I extract it
  Then `Answer.Value` is `new TimeOnly(14, 30)`
- AC5: date-time field returns a DateTime
  Given a provider that returns `"2026-10-03T14:30:00"` for a datetime field
  When I extract it
  Then `Answer.Value` is that `DateTime`
- AC6: duration field returns a number in the requested unit
  Given a duration field with `Unit = Days` and a provider that returns `14`
  When I extract it
  Then `Answer.Value` is `14`, in `Days`
- AC7: list field returns strings and their positions
  Given a list field and a provider that returns `mug` and `teapot` with two spans
  When I extract it
  Then `Answer.Values` is `["mug", "teapot"]`
- AC8: list positions line up with values
  Given the same list answer
  When I read `Answer.Spans`
  Then it has two spans, in the same order as `Values`
- AC9: several fields in one call
  Given a request with a text, a number and a date field
  When I call `GetExtractionResponseAsync(request)`
  Then `Answers` has exactly one entry per field id, each of the matching answer type
- AC10: value not in the content
  Given a provider that reports a field as not present
  When I extract it
  Then `Answer.Value` is null
- AC11: not found is flagged
  Given the same answer
  When I read `Answer.IsFound`
  Then it is false
- AC12: list not found is empty, not null
  Given a list field the provider reports as not present
  When I extract it
  Then `Answer.Values` is empty
- AC13: confidence and null probability are passed through
  Given a provider that returns confidence 0.99 and null probability 0.001
  When I extract a field
  Then `Answer.Confidence` is 0.99 and `Answer.NullProbability` is 0.001
- AC14: confidence is optional
  Given a provider that returns no confidence
  When I extract a field
  Then `Answer.Confidence` is null, with no exception
- AC15: model and usage are returned
  Given a provider that reports model `@cf/cloudflare/clairic` and 612 input tokens
  When I extract
  Then the response has that `ModelId` and `Usage.InputTokenCount = 612`
- AC16: reference date is passed on
  Given a request with `ReferenceDate = 2026-10-20`
  When I extract
  Then the provider client receives `ReferenceDate = 2026-10-20`
- AC17: profile by id
  Given an Extraction profile with a known id
  When I call `ExtractAsync(profileId, field, state)`
  Then the call runs against that profile
- AC18: profile by alias
  Given an Extraction profile with alias `cloudflare-clairic`
  When I call `ExtractAsync("cloudflare-clairic", field, state)`
  Then the call runs against that profile
- AC19: profile through the builder
  Given an Extraction profile with alias `cloudflare-clairic`
  When I call `ExtractAsync(b => b.WithProfile("cloudflare-clairic"), field, state)`
  Then the call runs against that profile
- AC20: stored default profile
  Given `DefaultExtractionProfileId` is set
  When I extract with no profile
  Then the call runs against the default Extraction profile
- AC21: default profile from config
  Given no stored default but `Umbraco:AI:DefaultExtractionProfileAlias` names an Extraction profile
  When I extract with no profile
  Then the call runs against that profile

**Sad path**

- AC22: no fields
  Given a request with zero fields
  When I extract
  Then `ArgumentException` is thrown
- AC23: blank field id
  Given a field whose id is blank
  When I extract
  Then `ArgumentException` is thrown
- AC24: duplicate field id
  Given two fields with id `orderId`
  When I extract
  Then `ArgumentException` is thrown
- AC25: invalid field id
  Given a field with id `1st-value` (starts with a digit, has a hyphen)
  When I extract
  Then `ArgumentException` is thrown
- AC26: field id too long
  Given a field id of 65 characters
  When I extract
  Then `ArgumentException` is thrown
- AC27: blank instructions
  Given a field with blank `Instructions`
  When I extract
  Then `ArgumentException` is thrown
- AC28: blank state
  Given a request with blank `State`
  When I extract
  Then `ArgumentException` is thrown
- AC29: decimals out of range
  Given a number field with `Decimals = 7`
  When I extract
  Then `ArgumentException` is thrown
- AC30: validation runs before the provider
  Given any invalid request from AC22 to AC29
  When I extract through the real client factory
  Then the provider client is never called
- AC31: validation is not a provider failure
  Given an invalid request
  When I extract through the real client factory
  Then the exception is `ArgumentException`, not `AIProviderException`
- AC32: validation records no usage
  Given an invalid request
  When I extract through the real client factory
  Then no usage record is written
- AC33: missing answer from the provider
  Given a provider that leaves out the answer for one field
  When I extract
  Then `AIProviderException` is thrown
- AC34: extra answer from the provider
  Given a provider that returns an answer for a field id that wasn't asked
  When I extract
  Then `AIProviderException` is thrown
- AC35: wrong answer type from the provider
  Given a provider that returns a text answer for a number field
  When I extract
  Then `AIProviderException` is thrown
- AC36: no profile resolves
  Given no stored or configured default Extraction profile
  When I extract with no profile
  Then `InvalidOperationException` says no default Extraction profile is set
- AC37: profile of the wrong capability
  Given a Chat profile's alias
  When I extract with it
  Then `InvalidOperationException` is thrown

## EX-2: Connect to Cloudflare

As a **site developer**,
I want to add a Cloudflare connection with my account ID and API token,
so that Extraction profiles can call Clairic.

**Happy path**

- AC1: provider is listed
  Given the `Umbraco.AI.Cloudflare` package is installed and the flag is on
  When providers are listed
  Then `cloudflare` ("Cloudflare") is in the list
- AC2: connection fields
  Given the Cloudflare provider
  When its settings are read
  Then they are Account ID (required), API token (required, sensitive) and Endpoint
- AC3: endpoint default
  Given a new Cloudflare connection with no endpoint set
  When it is used
  Then requests go to `https://api.cloudflare.com/client/v4`
- AC4: API token is masked and encrypted
  Given a saved Cloudflare connection
  When it is read back through the Management API
  Then the API token is masked, like other sensitive fields
- AC5: Extraction is the only capability
  Given the Cloudflare provider
  When its capabilities are read
  Then the only one is Extraction
- AC6: one model
  Given a Cloudflare connection
  When its Extraction models are listed
  Then the only model is `@cf/cloudflare/clairic`
- AC7: connection test passes
  Given a valid account ID and token
  When the connection is tested
  Then it passes
- AC8: connection test spends no tokens
  Given a connection test
  When it runs
  Then the only call made is `GET {Endpoint}/accounts/{AccountId}/ai/models/search`
- AC9: connection test is cached
  Given a connection test that passed
  When it runs again within an hour with the same token
  Then no new HTTP call is made
- AC10: request URL and auth
  Given an extraction call
  When the request is sent
  Then it is `POST {Endpoint}/accounts/{AccountId}/ai/run/@cf/cloudflare/clairic` with a Bearer token
- AC11: field mapping
  Given a number field `amount` with decimals 2, whole number on and unit `DKK`
  When the request is sent
  Then `fields.amount` has `type: "number"`, the instructions, `decimals: 2`, `integer: true` and `unit: "DKK"`
- AC12: list mapping
  Given a list field with item type Date
  When the request is sent
  Then its entry has `type: "list"` and `items: { type: "date" }`
- AC13: duration mapping
  Given a duration field with unit Days
  When the request is sent
  Then its entry has `type: "duration"` and `unit: "days"`
- AC14: state and reference date
  Given state text and a reference date of 2026-10-20
  When the request is sent
  Then the body has that `state` and `reference_date: "2026-10-20"`
- AC15: no images
  Given any extraction call
  When the request is sent
  Then the body has no `images`
- AC16: response mapping
  Given a Clairic answer with `value`, `confidence`, `p_null` and `span`
  When it is mapped
  Then they become `Value`, `Confidence`, `NullProbability` and `Span`
- AC17: span is end-exclusive
  Given a Clairic span `[6, 13]`
  When it is mapped
  Then `Span` is `Start = 6`, `End = 13`
- AC18: usage mapping
  Given `usage.input_tokens = 612` and `output_tokens = 0`
  When it is mapped
  Then `InputTokenCount = 612` and `OutputTokenCount = 0`
- AC19: wrapped response accepted
  Given a body wrapped in `{ result, success, errors }`
  When it is parsed
  Then the answers are read from `result`
- AC20: bare response accepted
  Given a body with `answers` at the top level
  When it is parsed
  Then the answers are read
- AC21: raw answer kept
  Given a Clairic answer that includes `guess`
  When it is mapped
  Then `RawRepresentation` still holds the `guess` value
- AC22: 429 is retried
  Given Cloudflare returns 429 once, then 200
  When I extract
  Then the call succeeds after 2 attempts

**Sad path**

- AC23: retries stop at 3
  Given Cloudflare returns 429 every time
  When I extract
  Then exactly 3 attempts are made
- AC24: retry wait is capped
  Given a 429 with `Retry-After: 120`
  When the client waits
  Then it waits no more than 30 seconds
- AC25: other errors are not retried
  Given Cloudflare returns 400
  When I extract
  Then only 1 attempt is made
- AC26: Cloudflare's error is in the message
  Given a 429 body with `errors: [{ code: 3040, message: "Capacity temporarily exceeded" }]` on every attempt
  When the final exception is thrown
  Then its message contains `3040` and `Capacity temporarily exceeded`
- AC27: wrong token fails the connection test
  Given an invalid token
  When the connection is tested
  Then it fails with Cloudflare's own error message
- AC28: missing account ID
  Given a connection with no account ID
  When it is saved
  Then it is rejected as a required field

## EX-3: Extraction stays hidden until turned on

As an **Umbraco AI maintainer**,
I want Extraction invisible and unusable until `Umbraco:AI:Experimental:Extraction` is on,
so that shipping it changes nothing for sites that haven't opted in.

**Happy path**

- AC1: capability number
  Given `AICapability`
  When it is inspected
  Then `Extraction` has value 9 and every existing value is unchanged
- AC2: off by default
  Given no configuration
  When `IsCapabilityEnabled(AICapability.Extraction)` is called
  Then it returns false
- AC3: on when set
  Given `Umbraco:AI:Experimental:Extraction = true`
  When `IsCapabilityEnabled(AICapability.Extraction)` is called
  Then it returns true
- AC4: everything works once on
  Given the flag is on
  When a connection's capabilities are listed, an Extraction profile is created, and the endpoint is called
  Then all three succeed

**Sad path (flag off)**

- AC5: not in a connection's capabilities
  Given a Cloudflare connection and the flag off
  When its capabilities are listed
  Then Extraction is not there
- AC6: no connections for the capability
  Given the flag off
  When connections are listed for Extraction
  Then the list is empty
- AC7: profile creation rejected
  Given the flag off
  When an Extraction profile is created
  Then `InvalidOperationException` tells the caller to turn on `Umbraco:AI:Experimental:Extraction`
- AC8: not in the enabled-capabilities list
  Given the flag off
  When `GET capabilities/enabled` is called
  Then Extraction is not in the result
- AC9: endpoint hidden
  Given the flag off
  When `POST extraction/extract` is called
  Then it returns 404
- AC10: service refuses
  Given the flag off
  When `IAIExtractionService` is called
  Then `InvalidOperationException` is thrown

## EX-4: Set a default Extraction profile

As a **site developer**,
I want to pick a default Extraction profile in Settings,
so that code and Automate steps can extract without naming a profile.

**Happy path**

- AC1: saved through the API
  Given an Extraction profile
  When `PUT settings` sets `defaultExtractionProfileId` to it
  Then `GET settings` returns that id
- AC2: picker shown when on
  Given the flag on
  When I open the AI Settings screen
  Then there is a "Default Extraction profile" picker
- AC3: picker only offers Extraction profiles
  Given a Chat profile and an Extraction profile
  When I open the picker
  Then only the Extraction profile is listed
- AC4: first run is empty
  Given no default set
  When I open the Settings screen
  Then the picker is empty

**Sad path**

- AC5: picker hidden when off
  Given the flag off
  When I open the Settings screen
  Then there is no Extraction picker
- AC6: hidden value survives a save
  Given a stored default Extraction profile and the flag off
  When I save Settings
  Then `defaultExtractionProfileId` is unchanged

## EX-5: Manage Extraction profiles in the backoffice

As a **site developer**,
I want Extraction to look like every other capability in the backoffice,
so that creating and reading an Extraction profile needs no new learning.

- AC1: label and icon
  Given the flag on
  When capabilities are listed anywhere in the backoffice
  Then Extraction shows as "Extraction" with icon `icon-filter`
- AC2: profile has no settings
  Given an Extraction profile
  When I open it
  Then the settings area shows the shared "No settings available" message
- AC3: profile round-trips
  Given a new Extraction profile on a Cloudflare connection
  When I save and reopen it
  Then it keeps its connection, model and empty settings
- AC4: usage dashboards
  Given an extraction call has run
  When I open the usage dashboard
  Then the call is listed under Extraction

## EX-6: Extract over the Management API

As a **backoffice extension developer**,
I want one endpoint that takes several fields and returns typed answers,
so that I can extract values from the backoffice without C#.

**Happy path**

- AC1: batch extract
  Given the request in `SPEC.md` "Management API surface"
  When I `POST /umbraco/ai/management/api/v1/extraction/extract`
  Then I get 200 with one answer per field
- AC2: answer types
  Given the same request
  When I read the response
  Then each answer's `$type` matches its field's `$type`
- AC3: field ids kept exactly
  Given a field id `deliveredOn`
  When I read the response
  Then the answer key is `deliveredOn`, not camel-cased or changed
- AC4: field id with capitals kept
  Given a field id `OrderID`
  When I read the response
  Then the answer key is `OrderID`
- AC5: profile by alias or id
  Given `profileIdOrAlias` set to an alias, then to an id
  When I call the endpoint
  Then both run against that profile
- AC6: default profile
  Given no `profileIdOrAlias` and a default Extraction profile
  When I call the endpoint
  Then it runs against the default
- AC7: model and usage
  Given a successful call
  When I read the response
  Then it has `modelId` and `usage`
- AC8: same auth as Decision
  Given a user who can call the Decision endpoint
  When they call the extract endpoint
  Then it is allowed

**Sad path**

- AC9: validation error
  Given a request with a duplicate field id
  When I call the endpoint
  Then I get 400 `ProblemDetails`
- AC10: no profile
  Given no profile in the request and no default
  When I call the endpoint
  Then I get 400 `ProblemDetails`
- AC11: provider error
  Given a provider that fails
  When I call the endpoint
  Then I get 400 `ProblemDetails`
- AC12: unauthenticated
  Given no backoffice token
  When I call the endpoint
  Then I get 401

## EX-7: Extract from TypeScript

As a **backoffice extension developer**,
I want a `UaiExtractionController` exported from `@umbraco-ai/core`,
so that my extension can extract values with typed answers and no hand-written HTTP.

- AC1: exported
  Given `@umbraco-ai/core`
  When I import `UaiExtractionController`
  Then it resolves and is in the public type rollup
- AC2: typed answers
  Given a request with a number and a list field
  When I call `extract(request)`
  Then the number answer has a numeric `value` and the list answer has a `values` array
- AC3: field kinds map to `$type`
  Given a field of kind `datetime`
  When the request is sent
  Then it goes out with `$type: "datetime"`
- AC4: no generated types leak
  Given the public type rollup
  When it is inspected
  Then no generated API model type is exported through the controller
- AC5: error surfaced
  Given the endpoint returns 400
  When I call `extract`
  Then the controller returns an error instead of answers
- AC6: v17 data source
  Given the v17 generated client, which has no base `$type`
  When I call `extract` on v17
  Then answers are typed the same way as on v18

## EX-8: Extraction calls are tracked like other capabilities

As a **site administrator**,
I want every extraction call recorded in usage, audit, notifications and telemetry,
so that I can see cost and failures the same way I do for chat.

- AC1: usage record
  Given a successful extraction
  When usage is read
  Then there is one record with capability Extraction and 612 input tokens
- AC2: audit entry
  Given a successful extraction
  When the audit log is read
  Then there is one entry with capability Extraction
- AC3: failed call audited
  Given a provider error
  When the audit log is read
  Then the entry is marked failed
- AC4: executing notification
  Given a handler for `AIExtractionExecutingNotification`
  When I extract
  Then the handler runs before the provider call
- AC5: cancel stops the call
  Given that handler cancels
  When I extract
  Then the provider is never called
- AC6: executed notification
  Given a handler for `AIExtractionExecutedNotification`
  When an extraction finishes
  Then the handler runs once, with the call marked successful
- AC7: one telemetry span
  Given an OpenTelemetry listener
  When I extract
  Then one span is emitted with the shared AI trace tags
- AC8: nested call gets its own context
  Given an extraction made inside another AI call
  When it runs
  Then it has its own runtime context

## EX-9: Set up fields on an Extract Values step

As an **Automate user**,
I want to list the values I want pulled out of some text,
so that each one becomes an output later steps can use.

**Happy path**

- AC1: empty state
  Given a new Extract Values step
  When I open its settings
  Then the Fields list shows the "No fields yet" message and an Add field button
- AC2: seven types to pick from
  Given the settings
  When I click Add field
  Then the picker offers Text, Number, Date, Time, Date and time, Duration and List
- AC3: picking a type opens the modal
  Given the picker
  When I pick Number
  Then the field modal opens with the headline "Add field: Number"
- AC4: number extras
  Given the Number modal
  When it opens
  Then it shows Alias, Instructions, Decimal places, Whole number and Unit
- AC5: duration extras
  Given the Duration modal
  When it opens
  Then it shows Unit (default seconds), Decimal places and Whole number
- AC6: list extras
  Given the List modal
  When it opens
  Then it shows Item type (Text, Number or Date; default Text)
- AC7: text, date, time and date-time have no extras
  Given one of those modals
  When it opens
  Then it shows only Alias and Instructions
- AC8: saved field appears in the list
  Given a filled-in field modal
  When I click Save
  Then the list shows a row with the instructions as its name and "Type · alias" under it
- AC9: edit keeps values
  Given a saved field
  When I click its row
  Then the modal opens with the headline "Edit field: <Type>" and the saved values
- AC10: remove
  Given a saved field
  When I click Remove
  Then the row is gone from the list
- AC11: order kept
  Given three saved fields
  When I save the step and reopen it
  Then the fields are in the same order

**Sad path**

- AC12: alias is required
  Given a field modal with an empty alias
  When I click Save
  Then the modal stays open with a message under Alias
- AC13: instructions are required
  Given a field modal with empty instructions
  When I click Save
  Then the modal stays open with a message under Instructions
- AC14: alias format
  Given the alias `1st-value`
  When I click Save
  Then the modal stays open with a message saying it must start with a letter and use only letters, numbers and `_`
- AC15: duplicate alias
  Given a list that already has `orderId`
  When I save another field with alias `orderId`
  Then the modal stays open with "Another field already uses the alias "orderId"."
- AC16: cancel discards
  Given an edited field
  When I click Cancel
  Then the saved field is unchanged
- AC17: cancel from add returns to the picker
  Given the Add field modal
  When I click Cancel
  Then the type picker is shown again
- AC18: more than 20 fields
  Given 21 fields
  When I save the step
  Then a validation message blocks the save

## EX-10: Extract values in an automation

As an **Automate user**,
I want an Extract Values step that reads text and gives me typed outputs,
so that later steps can create content, route or ask for approval from what was found.

**Happy path**

- AC1: action is listed
  Given the flag on
  When I add a step
  Then "Extract Values" is in the action list
- AC2: one output per alias
  Given fields `orderId`, `amount` and `damaged`
  When the step's output schema is read
  Then it has `orderId`, `amount` and `damaged`, each with `value`, `found` and `confidence`
- AC3: output schema types
  Given one field of each type
  When the output schema is read
  Then text, date, time and datetime are `string`; number and duration are `number`; list is an `array` of `string`
- AC4: found and confidence types
  Given any field
  When the output schema is read
  Then `found` is `boolean` and `confidence` is `["number", "null"]`
- AC5: values come out
  Given content "Order A-88412 ... 701,10 kr ..." and a working profile
  When the step runs
  Then `${steps.x.orderId.value}` is `A-88412`
- AC6: dates are ISO strings
  Given a date field found as 3 October 2026
  When the step runs
  Then its `value` is `"2026-10-03"`
- AC7: not found still has an output
  Given a field the content doesn't contain
  When the step runs
  Then its `found` is false
- AC8: not found value is empty
  Given the same field
  When the step runs
  Then its `value` is null
- AC9: content is bindable
  Given Content set to `${trigger.body}`
  When the step runs
  Then the trigger's body is the text extracted from
- AC10: reference date is bindable
  Given Reference date set to a binding that resolves to `2026-10-20`
  When the step runs
  Then the provider receives reference date 2026-10-20
- AC11: empty reference date uses the run date
  Given an empty Reference date
  When the step runs
  Then the provider receives today's date (UTC)
- AC12: chosen profile
  Given a profile picked on the step
  When the step runs
  Then that profile is used
- AC13: default profile
  Given no profile picked
  When the step runs
  Then the default Extraction profile is used
- AC14: recipe works with If
  Given an If step after Extract Values checking `${steps.x.orderId.found}` equals `true`
  When the automation runs on text containing an order number
  Then the true branch runs

**Sad path**

- AC15: flag off hides the action
  Given the flag off
  When I add a step
  Then "Extract Values" is not in the action list
- AC16: flag off fails an existing step
  Given an automation that already has the step, and the flag off
  When it runs
  Then the step fails with a validation error "Extraction is disabled."
- AC17: empty content
  Given Content that resolves to empty
  When the step runs
  Then it fails with a validation error
- AC18: bad reference date
  Given Reference date resolving to `next week`
  When the step runs
  Then it fails with a validation error
- AC19: no fields
  Given a step saved with no fields
  When it runs
  Then it fails with a validation error
- AC20: no profile
  Given no profile picked and no default
  When the step runs
  Then it fails with a validation error
- AC21: provider error
  Given a provider that fails
  When the step runs
  Then it fails with the category from `StepErrorCategoryMapping`

## EX-11: Build the idea's recipes with no code

As a **solution partner selling Automate**,
I want the headline recipes to work end to end on a real site,
so that I can demo them to a customer.

- AC1: webhook to content
  Given a Webhook trigger, Extract Values and Create Content on the demo site
  When a lead email body is posted to the webhook
  Then a content item is created with the extracted name, company and date
- AC2: ticket triage with approval
  Given Webhook → Extract Values → Ask Pick-One → If on confidence → Request Approval
  When a ticket with an unclear amount is posted
  Then the approval step is reached
- AC3: transcript to content
  Given Transcribe Audio → Extract Values → Create Content
  When a voice note is uploaded
  Then a content item is created with the extracted values
- AC4: numbers on a Danish server
  Given the demo site running with culture `da-DK`
  When an If compares `${steps.x.amount.confidence}` greater than `0.8` on a 0.99 answer
  Then the result is recorded in `BUILD-LOG.md`
  > ASSUMPTION: This checks the known culture risk in `SPEC.md`. It records the result rather than
  > requiring a pass, because the fix belongs to Automate.

## EX-12: Deploy carries Extraction setup

As a **site developer using Umbraco Deploy**,
I want my Cloudflare connection, Extraction profiles and default profile to move between
environments,
so that I don't set them up again on every environment.

- AC1: profile round-trips
  Given an Extraction profile
  When it is exported and imported by `UmbracoAIProfileServiceConnector`
  Then it arrives with capability Extraction and empty settings
- AC2: default exported
  Given a default Extraction profile
  When settings are exported
  Then the artifact has `DefaultExtractionProfileUdi`
- AC3: default is a dependency
  Given the same export
  When its dependencies are read
  Then the Extraction profile is one of them
- AC4: default imported
  Given an artifact with `DefaultExtractionProfileUdi`
  When it is imported
  Then `DefaultExtractionProfileId` is set to that profile
- AC5: older artifact still imports
  Given a settings artifact with no `DefaultExtractionProfileUdi`
  When it is imported
  Then the import succeeds and the default stays null

## EX-13: Same feature on v17

As an **Automate customer on the LTS line**,
I want Extraction on Umbraco 17 too,
so that I don't have to move to 18 to use it.

- AC1: port branch
  Given the finished v18 work
  When it is ported
  Then it lands on `v17/feature/extraction-capability` as a PR into `v17/dev`
- AC2: builds and tests
  Given the port
  When every touched product is built and tested
  Then all are green
- AC3: same capability number
  Given v17's `AICapability`
  When it is inspected
  Then `Extraction` is 9
- AC4: works live
  Given the v17 demo site with a real Cloudflare token
  When `POST extraction/extract` is called
  Then it returns typed answers

## EX-14: Public docs

As a **developer reading Umbraco's docs**,
I want Extraction, the Cloudflare provider and the Extract Values action documented,
so that I can set it up without reading the source.

- AC1: both lines
  Given Umbraco.Docs
  When the pages are added
  Then they exist under both `17/` and `18/`
- AC2: marked experimental
  Given each page
  When it is read
  Then it says the feature is experimental and names the flag
- AC3: one recipe
  Given the Automate page
  When it is read
  Then it walks through at least one recipe end to end
