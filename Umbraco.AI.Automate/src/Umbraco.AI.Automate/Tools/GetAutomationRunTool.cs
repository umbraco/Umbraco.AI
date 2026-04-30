using System.ComponentModel;
using Umbraco.AI.Automate.Tools.Scopes;
using Umbraco.AI.Core.Tools;
using Umbraco.Automate.Core.Runs;

namespace Umbraco.AI.Automate.Tools;

/// <summary>
/// Arguments for the GetAutomationRun tool.
/// </summary>
/// <param name="RunId">The unique identifier of the automation run to check.</param>
public record GetAutomationRunArgs(
    [property: Description("The unique identifier (GUID) of the automation run to check. Use the run ID returned by run_automation.")]
    Guid RunId);

/// <summary>
/// Tool that checks the status and progress of a previously triggered automation run.
/// </summary>
[AITool("get_automation_run", "Get Automation Run", ScopeId = AutomateReadScope.ScopeId)]
public class GetAutomationRunTool : AIToolBase<GetAutomationRunArgs>
{
    private readonly IAutomationRunService _runService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetAutomationRunTool"/> class.
    /// </summary>
    public GetAutomationRunTool(IAutomationRunService runService)
    {
        _runService = runService;
    }

    /// <inheritdoc />
    public override string Description =>
        "Checks the status and progress of an automation run. " +
        "Use the run ID returned by run_automation to check if the automation has completed, " +
        "is still running, or has failed.";

    /// <inheritdoc />
    protected override async Task<object> ExecuteAsync(GetAutomationRunArgs args, CancellationToken cancellationToken = default)
    {
        if (args.RunId == Guid.Empty)
        {
            return new GetAutomationRunResult(false, null, null, [], null, "Run ID cannot be empty.");
        }

        AutomationRun? run = await _runService.GetRunAsync(args.RunId, cancellationToken);
        if (run is null)
        {
            return new GetAutomationRunResult(false, null, null, [], null, $"Run '{args.RunId}' not found.");
        }

        var stepResults = run.StepRuns
            .Select(s => new StepRunItem(
                s.ActionAlias,
                s.Status.ToString(),
                s.Duration?.TotalSeconds,
                s.Error))
            .ToList();

        return new GetAutomationRunResult(
            true,
            run.Id,
            run.Status.ToString(),
            stepResults,
            run.Error,
            null);
    }
}

/// <summary>
/// Result of the get automation run tool.
/// </summary>
/// <param name="Success">Whether the run was found.</param>
/// <param name="RunId">The run ID.</param>
/// <param name="Status">The overall run status (Pending, Running, Completed, Failed, Suspended, Cancelled).</param>
/// <param name="Steps">The individual step results.</param>
/// <param name="Error">The error message if the run failed.</param>
/// <param name="Message">Optional message (typically for lookup errors).</param>
public record GetAutomationRunResult(
    bool Success,
    Guid? RunId,
    string? Status,
    IReadOnlyList<StepRunItem> Steps,
    string? Error,
    string? Message);

/// <summary>
/// A single step's execution status within an automation run.
/// </summary>
/// <param name="ActionAlias">The action that was executed.</param>
/// <param name="Status">The step status (Pending, Running, Completed, Failed, Skipped, WaitingForInput, Sleeping).</param>
/// <param name="DurationSeconds">The execution duration in seconds, if completed.</param>
/// <param name="Error">The error message if the step failed.</param>
public record StepRunItem(
    string ActionAlias,
    string Status,
    double? DurationSeconds,
    string? Error);
