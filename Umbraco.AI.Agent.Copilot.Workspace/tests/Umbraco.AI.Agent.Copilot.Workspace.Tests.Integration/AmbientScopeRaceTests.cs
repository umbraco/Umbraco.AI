// ---------------------------------------------------------------------------------------------
// DELIBERATE DEVIATION FROM REPO CONVENTION: this project (and only this project) uses NUnit +
// Shouldly instead of the repo-default xUnit stack. That is because it must run on top of
// Umbraco CMS's OWN scope-testing harness, `UmbracoIntegrationTest` (shipped via the
// `Umbraco.Cms.Tests.Integration` / `Umbraco.Cms.Tests` NuGet packages), which is NUnit-based
// with no xUnit equivalent. See root CLAUDE.md for the repo's normal test conventions.
//
// WHAT THIS PROVES: https://github.com/umbraco/Umbraco.AI/issues/375
//
// Copilot Workspace chat conversations intermittently die mid-stream with:
//   System.InvalidOperationException: The Scope {A} being disposed is not the Ambient Scope {B}
//     at Umbraco.Cms.Infrastructure.Scoping.Scope.Dispose()
//     at Umbraco.Cms.Persistence.EFCore.Scoping.EFCoreScope`1.Dispose()
//     at ...EFCoreAIConversationRepository.GetSessionStateJsonAsync(...)
//
// Diagnosed root cause class: in AIAgentService.cs, three call sites start
// `StartLoadSessionStateAsync(...)` (which opens an EF Core scope) without awaiting it, run
// `PrepareAgentExecutionAsync(...)` - which opens its own, separate EF Core scope - and only
// await the first task much later. `AmbientEFCoreScopeStack<TDbContext>`
// (Umbraco.Cms.Persistence.EFCore.Scoping) backs its ambient-scope tracking with
// `private static AsyncLocal<ConcurrentStack<IEfCoreScope<TDbContext>>>` - a MUTABLE reference
// type stored in an AsyncLocal. Once populated, that ConcurrentStack instance can end up SHARED
// between two independently-progressing flows that both push/pop against it, violating the
// strict LIFO ordering the ambient-scope stack assumes for purely sequential nested `using`
// scopes. Whichever scope disposes while it is no longer the top of the stack throws exactly the
// InvalidOperationException above.
//
// EMPIRICAL NOTE (found while building this test, see #375 for the full writeup): a plain
// `var sessionStateTask = SomeAsyncMethod(...)` call that is not immediately awaited does NOT,
// on its own, leak that method's AsyncLocal-based scope push back into the caller - confirmed
// with a minimal, isolated repro (an AsyncLocal<int> set inside an async method's synchronous
// prefix, before its first await, is NOT visible to the caller after the un-awaited call
// returns). The .NET async state-machine isolates a callee's ambient-context mutations from its
// caller on this synchronous "prefix" return path, even without any suspension yet. So the exact
// literal shape in AIAgentService.cs cannot be reproduced by calling two sibling `async Task`
// methods directly, no matter how their internal delays are tuned - there is nothing to
// interleave until real thread-level sharing happens.
//
// What genuinely produces the shared, racing ConcurrentStack (and is explicitly named as a
// trigger in the exception message itself: "...or flowed to a child thread that was not
// awaited, or concurrent threads are accessing the same Scope...") is dispatching the
// session-state load onto its own thread-pool work item via Task.Run, which - unlike a plain
// async method call - FLOWS (copies by reference, not isolates) the ambient ExecutionContext
// into the new thread. This is exactly the mechanism Umbraco CMS's own scope test suite uses to
// reliably trigger this same exception via real scope machinery (see
// EFCoreScopeTest.GivenChildThread_WhenParentDisposedBeforeChild_ParentScopeThrows in the CMS
// source). This test uses the same technique: a TaskCompletionSource handshake guarantees the
// background scope is genuinely open and ambient before the second, awaited scope is created, so
// the resulting cross-flow LIFO violation - and the resulting InvalidOperationException - is
// deterministic rather than a rare timing fluke, while every other aspect of the shape ("start
// task 1, don't await; run + fully await task 2; only then await task 1") matches
// AIAgentService.cs.
//
// This test does NOT use Umbraco.AI.Tests.Common's TestEFCoreScopeProvider fake (no ambient
// tracking at all - it would stay green on this bug forever). It drives the REAL
// IEFCoreScopeProvider<T> / IEfCoreScope<T> obtained from a real UmbracoIntegrationTest host,
// using a throwaway DummyConversationDbContext that mirrors how EFCoreAIConversationRepository
// is wired. It intentionally does NOT modify any production code - proof before fix only.
// ---------------------------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Persistence.EFCore.Scoping;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;
using Umbraco.Extensions;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Integration;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewEmptyPerTest)]
internal sealed class AmbientScopeRaceTests : UmbracoIntegrationTest
{
    /// <summary>
    /// Umbraco.Cms.Tests.Integration's own assembly-level `[SetUpFixture]` (`GlobalSetupTeardown`,
    /// which normally reads `appsettings.Tests.json` to populate `Tests:Database:DatabaseType`
    /// etc.) is never discovered/run here - NUnit only runs `[SetUpFixture]`s declared in the
    /// assembly actually under test, not ones living in a referenced NuGet package's assembly.
    /// Supply the same test-database settings directly instead.
    /// </summary>
    protected override void SetUpTestConfiguration(IConfigurationBuilder configBuilder)
    {
        base.SetUpTestConfiguration(configBuilder);

        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Tests:Database:DatabaseType"] = "Sqlite",
            ["Tests:Database:PrepareThreadCount"] = "4",
            ["Tests:Database:SchemaDatabaseCount"] = "4",
            ["Tests:Database:EmptyDatabasesCount"] = "2",
        });
    }

    /// <summary>
    /// Registers a throwaway <see cref="DummyConversationDbContext"/> through the SAME public
    /// registration path production code uses (`AddUmbracoDbContext&lt;T&gt;`), which wires up
    /// the real `IEFCoreScopeProvider&lt;T&gt;`, `IEFCoreScopeAccessor&lt;T&gt;` and ambient
    /// scope-stack tracking - not a hand-rolled substitute.
    /// </summary>
    protected override void CustomTestSetup(IUmbracoBuilder builder)
    {
        base.CustomTestSetup(builder);

        builder.Services.AddUmbracoDbContext<DummyConversationDbContext>(
            (DbContextOptionsBuilder options, string? connectionString, string? providerName, IServiceProvider? _) =>
            {
                options.UseDatabaseProvider(providerName!, connectionString!);
            },
            // shareUmbracoConnection: false - each scope gets its own physical connection so real
            // concurrent SQLite access from the two racing flows doesn't hit unrelated connection
            // contention/locking; only the ambient EFCoreScope-stack tracking itself is shared,
            // which is what this test is about.
            shareUmbracoConnection: false);
    }

    // Named distinctly from the base UmbracoIntegrationTest.ScopeProvider (which exposes the
    // unrelated, non-generic core IScopeProvider) to avoid member hiding.
    private IEFCoreScopeProvider<DummyConversationDbContext> DummyScopeProvider =>
        GetRequiredService<IEFCoreScopeProvider<DummyConversationDbContext>>();

    /// <summary>
    /// Mirrors <c>EFCoreAIConversationRepository.GetSessionStateJsonAsync</c> /
    /// <c>AIAgentService.StartLoadSessionStateAsync</c>: opens a scope, does some "DB work" with a
    /// real await inside it, completes and disposes the scope. <paramref name="scopeOpened"/> is
    /// signalled right after the scope is created (and therefore pushed onto the ambient stack),
    /// so the caller can deterministically wait for that before doing anything else - see the
    /// EMPIRICAL NOTE at the top of this file for why that handshake (rather than tuned delays
    /// alone) is what makes this reliable.
    /// </summary>
    private async Task<string?> SimulateLoadSessionStateAsync(
        TaskCompletionSource scopeOpened,
        CancellationToken cancellationToken)
    {
        // NOTE: this pinned CMS version (17.5.0) names the scope type `IEfCoreScope<T>` (lowercase
        // "f") - later CMS versions rename it to `IEFCoreScope<T>`. Matched to the restored package.
        using IEfCoreScope<DummyConversationDbContext> scope = DummyScopeProvider.CreateScope();
        Console.WriteLine($"[SessionState] scope created: InstanceId={scope.InstanceId} Depth={scope.Depth}");
        scopeOpened.TrySetResult();

        // Deliberately does NOT call ExecuteWithContextAsync/touch the DbContext: doing so opens a
        // real SQLite transaction, and two genuinely concurrent transactions against the same
        // SQLite file (one per racing flow, even on separate connections) contend for SQLite's
        // own file-level lock and throw an unrelated SqliteException ("database table is locked")
        // before the ambient-scope race even gets a chance to manifest. The bug under test lives
        // entirely in scope creation/disposal bookkeeping, not in data access, so a plain await is
        // enough to force the genuine suspend/resume this needs.
        //
        // Short delay: this flow's own continuation resumes (and tries to complete/dispose its
        // scope) WHILE SimulatePrepareAgentExecutionAsync's much longer-running scope below is
        // still open - that is the exact "one flow completes while another interleaved scope is
        // still on top of the shared ambient stack" shape that trips the real bug.
        await Task.Delay(10, cancellationToken);

        scope.Complete();
        return "session-state-json"; // The `using` block's Dispose() below is where the real bug throws.
    }

    /// <summary>
    /// Mirrors <c>AIAgentService.PrepareAgentExecutionAsync</c> -&gt;
    /// <c>_agentFactory.CreateAgentAsync</c>: its OWN, separate scoped DB work, fully awaited by
    /// the caller before it goes back to await the session-state task.
    /// </summary>
    private async Task SimulatePrepareAgentExecutionAsync(CancellationToken cancellationToken)
    {
        using IEfCoreScope<DummyConversationDbContext> scope = DummyScopeProvider.CreateScope();
        Console.WriteLine($"[PrepareAgent] scope created: InstanceId={scope.InstanceId} Depth={scope.Depth}");

        // Deliberately much longer than SimulateLoadSessionStateAsync's delay above, so this scope
        // is still open on the shared ambient stack when the other flow's scope tries to complete.
        // See the comment in SimulateLoadSessionStateAsync for why this skips ExecuteWithContextAsync.
        await Task.Delay(200, cancellationToken);

        scope.Complete();
    }

    /// <summary>
    /// Reproduces the application race described in #375, using real CMS scope machinery
    /// throughout: "start task 1, don't await; run + fully await task 2; only then await task 1".
    /// See the EMPIRICAL NOTE at the top of this file for why task 1 is dispatched via Task.Run.
    /// </summary>
    [Test]
    [Repeat(8)]
    public async Task ConcurrentSessionStateLoad_And_PrepareAgentExecution_ReproducesAmbientScopeRace()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var scopeOpened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        // An outer ambient scope, already open BEFORE task 1 and task 2 fork off it, is required
        // for them to genuinely share the SAME underlying ConcurrentStack instance backing
        // AmbientEFCoreScopeStack<DummyConversationDbContext>: `_stack.Value ??= new
        // ConcurrentStack<...>()` only creates that instance on the FIRST push for a given
        // execution-context branch. If task 1 and task 2 each independently hit that first-ever
        // push (nothing open yet for this DbContext type), each gets its OWN separate
        // ConcurrentStack instance and never interleaves at all - which is what an earlier
        // iteration of this test observed (both scopes landing at Depth 1, never Depth 2, i.e.
        // never actually nesting under one another). Opening this outer scope first means task 1's
        // and task 2's own CreateScope() calls both observe a non-null, already-materialized
        // ConcurrentStack and nest into the SAME shared instance, which is what the real
        // production code's ambient conversation/profile scopes already do implicitly (nested
        // inside whatever ambient Umbraco scope the current HTTP request/service-graph call
        // already opened).
        IEfCoreScope<DummyConversationDbContext> outerScope = DummyScopeProvider.CreateScope();
        Console.WriteLine($"[Outer] scope created: InstanceId={outerScope.InstanceId} Depth={outerScope.Depth}");

        try
        {
            // Step 1: kick off the "load session state" work but do NOT await it yet - exactly
            // like `var sessionStateTask = StartLoadSessionStateAsync(...)` in AIAgentService.cs.
            // Dispatched via Task.Run so its ambient scope genuinely flows onto the shared ambient
            // stack (see the EMPIRICAL NOTE at the top of this file).
            Task<string?> sessionStateTask = Task.Run(
                () => SimulateLoadSessionStateAsync(scopeOpened, cts.Token),
                cts.Token);

            // Deterministically wait until the session-state scope is genuinely open/ambient
            // before proceeding - not a delay-based guess.
            await scopeOpened.Task;

            // Step 2: immediately run AND fully await a second, independent scoped DB operation -
            // exactly like `var context = await PrepareAgentExecutionAsync(...)`.
            await SimulatePrepareAgentExecutionAsync(cts.Token);

            // Step 3: only now await the first task - exactly like
            // `session = await CreateOrRestoreSessionAsync(..., sessionStateTask, ...)`.
            InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(async () =>
                await sessionStateTask);

            Console.WriteLine($"Reproduced #375: {ex.Message}");
            Console.WriteLine(ex.StackTrace);

            // This is the real production crash message shape (either the EFCoreScope-level check
            // or, as in the reported stack trace, the wrapped core Scope's own check - both are
            // the same ambient-stack-violated-by-concurrent-flows failure).
            ex.Message.ShouldContain("is not the Ambient");
            ex.Message.ShouldContain("Scope");
        }
        finally
        {
            // The reproduced bug can leave the ambient stack in an inconsistent state (that IS the
            // bug), so cleanup here is best-effort and must not mask the assertion above with a
            // secondary exception.
            try
            {
                outerScope.Complete();
                outerScope.Dispose();
            }
            catch (InvalidOperationException cleanupEx)
            {
                Console.WriteLine($"[Outer] cleanup dispose also failed (expected fallout of the bug): {cleanupEx.Message}");
            }
        }
    }

    /// <summary>
    /// Proves the fix applied to AIAgentService.cs for #375: the session-state load is now fully
    /// awaited BEFORE the other scoped work starts (no more "start task 1, don't await; run task
    /// 2; only then await task 1"). With the two scoped operations strictly sequential rather than
    /// overlapping, they never share the ambient stack while both are open, so no
    /// InvalidOperationException occurs and both complete cleanly - same real CMS scope machinery,
    /// same outer-ambient-scope setup as the race test above, only the ordering changed.
    /// </summary>
    [Test]
    [Repeat(8)]
    public async Task SequentialSessionStateLoad_ThenPrepareAgentExecution_DoesNotRace()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var scopeOpened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        IEfCoreScope<DummyConversationDbContext> outerScope = DummyScopeProvider.CreateScope();
        Console.WriteLine($"[Outer] scope created: InstanceId={outerScope.InstanceId} Depth={outerScope.Depth}");

        try
        {
            // Fixed shape: await the session-state load fully to completion FIRST - mirrors
            // `var persistedState = await StartLoadSessionStateAsync(...)` in AIAgentService.cs.
            string? persistedState = await SimulateLoadSessionStateAsync(scopeOpened, cts.Token);
            persistedState.ShouldBe("session-state-json");

            // Only then start the other scoped work - mirrors
            // `var context = await PrepareAgentExecutionAsync(...)` now running after, not
            // alongside, the session-state load.
            await Should.NotThrowAsync(() => SimulatePrepareAgentExecutionAsync(cts.Token));
        }
        finally
        {
            outerScope.Complete();
            outerScope.Dispose();
        }
    }
}
