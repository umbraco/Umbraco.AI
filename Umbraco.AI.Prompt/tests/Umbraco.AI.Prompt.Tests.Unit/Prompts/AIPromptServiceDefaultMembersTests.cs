// Public IAIPromptService additions must not break external implementers

using Shouldly;

using Umbraco.AI.Prompt.Core.Prompts;

using Xunit;

namespace Umbraco.AI.Prompt.Tests.Unit.Prompts;

/// <summary>
/// An implementer written against the old IAIPromptService surface still compiles, and the includeRestricted
/// members added since fail closed.
/// </summary>
public class AIPromptServiceDefaultMembersTests
{
    private readonly OldSurfacePromptService _old = new();

    private IAIPromptService Service => _old;

    [Fact]
    public async Task GetPromptAsync_WithIncludeRestricted_IgnoresTheFlag()
    {
        // Given an implementer of the old surface whose filtering getters hide the prompt
        // When  the includeRestricted overloads are called with true
        var byId = await Service.GetPromptAsync(Guid.NewGuid(), includeRestricted: true);
        var byAlias = await Service.GetPromptByAliasAsync("limited", includeRestricted: true);
        var paged = await Service.GetPromptsPagedAsync(0, 10, null, null, includeRestricted: true);

        // Then  the restricted prompt is still hidden, and the old getters were the ones asked
        byId.ShouldBeNull();
        byAlias.ShouldBeNull();
        paged.Items.ShouldBeEmpty();
        _old.GetPromptCalls.ShouldBe(1);
        _old.GetPromptByAliasCalls.ShouldBe(1);
        _old.GetPromptsPagedCalls.ShouldBe(1);
    }

    [Fact]
    public async Task GetPromptForUse_FallsBackToTheFilteringGetters()
    {
        // Given an implementer of the old surface whose filtering getters hide the prompt
        // When  the for-use members are called
        var byId = await Service.GetPromptForUseAsync(Guid.NewGuid());
        var byAlias = await Service.GetPromptForUseByAliasAsync("limited");

        // Then  the prompt reads as missing, and the old getters were the ones asked
        byId.ShouldBeNull();
        byAlias.ShouldBeNull();
        _old.GetPromptCalls.ShouldBe(1);
        _old.GetPromptByAliasCalls.ShouldBe(1);
    }

    /// <summary>Implements only the members that existed before includeRestricted.</summary>
    private sealed class OldSurfacePromptService : IAIPromptService
    {
        public int GetPromptCalls { get; private set; }

        public int GetPromptByAliasCalls { get; private set; }

        public int GetPromptsPagedCalls { get; private set; }

        public Task<AIPrompt?> GetPromptAsync(Guid id, CancellationToken cancellationToken = default)
        {
            GetPromptCalls++;
            return Task.FromResult<AIPrompt?>(null);
        }

        public Task<AIPrompt?> GetPromptByAliasAsync(string alias, CancellationToken cancellationToken = default)
        {
            GetPromptByAliasCalls++;
            return Task.FromResult<AIPrompt?>(null);
        }

        public Task<IEnumerable<AIPrompt>> GetPromptsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<AIPrompt>>([]);

        public Task<(IEnumerable<AIPrompt> Items, int Total)> GetPromptsPagedAsync(
            int skip,
            int take,
            string? filter = null,
            Guid? profileId = null,
            CancellationToken cancellationToken = default)
        {
            GetPromptsPagedCalls++;
            return Task.FromResult<(IEnumerable<AIPrompt> Items, int Total)>(([], 0));
        }

        public Task<AIPrompt> SavePromptAsync(AIPrompt prompt, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> DeletePromptAsync(Guid id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> PromptsExistWithProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<bool> PromptAliasExistsAsync(string alias, Guid? excludeId = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AIPromptExecutionResult> ExecutePromptAsync(
            Guid promptId,
            AIPromptExecutionRequest request,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<AIPromptExecutionResult> ExecutePromptAsync(
            Guid promptId,
            AIPromptExecutionRequest request,
            AIPromptExecutionOptions options,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
