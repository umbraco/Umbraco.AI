# Two-step rename script for root product folders
# Required for case-insensitive filesystems (Windows/macOS)

$renames = @(
    @("Umbraco.Ai", "Umbraco.AI"),
    @("Umbraco.Ai.Agent", "Umbraco.AI.Agent"),
    @("Umbraco.Ai.Prompt", "Umbraco.AI.Prompt"),
    @("Umbraco.Ai.OpenAi", "Umbraco.AI.OpenAI"),
    @("Umbraco.Ai.Anthropic", "Umbraco.AI.Anthropic"),
    @("Umbraco.Ai.Amazon", "Umbraco.AI.Amazon"),
    @("Umbraco.Ai.Google", "Umbraco.AI.Google"),
    @("Umbraco.Ai.MicrosoftFoundry", "Umbraco.AI.MicrosoftFoundry"),
    @("Umbraco.Ai.Agent.Copilot", "Umbraco.AI.Agent.Copilot"),
    @("demo/Umbraco.Ai.DemoSite", "demo/Umbraco.AI.DemoSite")
)

foreach ($rename in $renames) {
    $old = $rename[0]
    $new = $rename[1]
    $temp = "$old.TEMP"

    Write-Host "Renaming $old -> $new" -ForegroundColor Cyan

    # Step 1: Rename to temp
    if (Test-Path $old) {
        git mv $old $temp
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to rename $old to $temp"
            exit 1
        }
    } else {
        Write-Warning "Path not found: $old"
        continue
    }

    # Step 2: Rename from temp to final
    git mv $temp $new
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to rename $temp to $new"
        exit 1
    }

    Write-Host "✓ Completed: $old -> $new" -ForegroundColor Green
}

Write-Host "`nAll folders renamed successfully!" -ForegroundColor Green
Write-Host "Creating commit..." -ForegroundColor Cyan

git commit -m "refactor: Rename Ai to AI in root product folders"

Write-Host "✓ Commit created" -ForegroundColor Green
