# Local Umbraco.AI build — culture-variant prompt fix

Built from branch `feature/Prompt-Culture-Variant`.

## What's in this folder

NuGet packages built from local source, version **`1.10.1--preview.2.g7aae44e`**:

- `Umbraco.AI` — meta-package (pulls in everything else via deps)
- `Umbraco.AI.Core` — backend culture-aware property selection
- `Umbraco.AI.Web.StaticAssets` — frontend adapters with active-variant filtering
- `Umbraco.AI.Web`, `.Startup`, `.Persistence`, `.Persistence.SqlServer`, `.Persistence.Sqlite`

`Umbraco.AI.Prompt` does not need rebuilding — its dependency range `[1.10.0, 1.999.999)` accepts this version.

## What the fix does

Multi-variant documents previously sent the wrong-culture value when an AI prompt
referenced `{{propertyAlias}}`. Now the frontend filters property values by the
active variant before serializing, ships `culture`/`segment` per property, and the
backend `AIEntityContextHelper.BuildContextDictionary` picks the matching entry
(falling back to the invariant entry, then the last entry).

Files changed:
- Frontend: `entity-adapter/types.ts`, `adapters/document.adapter.ts`, `adapters/block.adapter.ts`, `adapters/media.adapter.ts`
- Backend: `EntityAdapter/AISerializedEntity.cs`, `EntityAdapter/AIEntityContextHelper.cs`, `RuntimeContext/Contributors/SerializedEntityContributor.cs`, `RuntimeContext/Contributors/SerializedElementContributor.cs`
- Tests: `AIEntityContextHelperTests.cs` (5 new), `SerializedEntityContributorTests.cs` (1 new)

## Install in your local Umbraco project

1. Stop the Umbraco site.
2. Add the local feed (next to your `.csproj` or solution):

   ```xml
   <!-- nuget.config -->
   <?xml version="1.0" encoding="utf-8"?>
   <configuration>
     <packageSources>
       <add key="local-umbraco-ai" value="C:\Projects\Umbraco-AI\local-feed" />
     </packageSources>
   </configuration>
   ```

   Or: `dotnet nuget add source C:\Projects\Umbraco-AI\local-feed -n local-umbraco-ai`

3. Update the package:

   ```bash
   dotnet add package Umbraco.AI --version 1.10.1--preview.2.g7aae44e
   ```

   In Visual Studio: enable "Include prerelease", select the local source, update.

4. `dotnet restore && dotnet build && dotnet run`.

## Verifying the fix

1. Content type with two text properties varying by culture (e.g. `header`, `text`).
2. Populate a document in `en-US`, `da-DK`, `sv-SE`, `de-DE` with distinctly different content.
3. Open the Swedish variant, open DevTools → Network, trigger an AI prompt that uses `{{header}}` / `{{text}}`.
4. Request payload should show `culture: "sv-SE"` on the entity, and one entry per alias under `data.properties` carrying `culture: "sv-SE"`. Pre-fix it was unfiltered — multiple entries per alias, last one wins.
5. Model output should match the Swedish content.
6. Cycle through other variants — each must use its own culture.
7. Invariant content (no varying properties) must still resolve normally.

## Rebuilding after changes

```bash
cd C:\Projects\Umbraco-AI
npm run build:core                                       # rebuild frontend (writes to wwwroot)
dotnet pack Umbraco.AI/Umbraco.AI.slnx -c Release -o local-feed
```

The version will increment automatically (Nerdbank.GitVersioning uses commit height + short sha). To force NuGet to pick up a re-pack at the same version, clear the http cache: `dotnet nuget locals http-cache --clear`.

## Source repo

`C:\Projects\Umbraco-AI` — branch `feature/Prompt-Culture-Variant`.
