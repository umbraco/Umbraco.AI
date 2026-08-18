using Moq;
using Shouldly;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Core.FileStore;
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Conversations;

/// <summary>
/// This is the vote the file store's retention sweep relies on to keep a persisted conversation's
/// attachments alive past the fixed retention window. Archived is the case that motivated it: an
/// archived conversation is still readable, so it must report Alive exactly like an active one.
/// </summary>
public class ConversationFileThreadLifecycleProviderTests
{
    [Fact]
    public async Task GetStatusAsync_ExistingConversation_ReturnsAlive()
    {
        var id = Guid.NewGuid();
        var repository = new Mock<IAIConversationRepository>();
        repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(new AIConversation { Id = id });
        var provider = new ConversationFileThreadLifecycleProvider(repository.Object);

        var status = await provider.GetStatusAsync(id.ToString());

        status.ShouldBe(AIFileThreadLifecycleStatus.Alive);
    }

    [Fact]
    public async Task GetStatusAsync_ArchivedConversation_StillReturnsAlive()
    {
        // Archiving is a field toggle, not a delete — the row (and GetByIdAsync) doesn't care about it.
        // This test exists because "archived but still readable" is exactly the case the fixed-age sweep
        // used to break.
        var id = Guid.NewGuid();
        var repository = new Mock<IAIConversationRepository>();
        repository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIConversation { Id = id, IsArchived = true });
        var provider = new ConversationFileThreadLifecycleProvider(repository.Object);

        var status = await provider.GetStatusAsync(id.ToString());

        status.ShouldBe(AIFileThreadLifecycleStatus.Alive);
    }

    [Fact]
    public async Task GetStatusAsync_UnknownConversationId_ReturnsGone()
    {
        var id = Guid.NewGuid();
        var repository = new Mock<IAIConversationRepository>();
        repository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((AIConversation?)null);
        var provider = new ConversationFileThreadLifecycleProvider(repository.Object);

        var status = await provider.GetStatusAsync(id.ToString());

        status.ShouldBe(AIFileThreadLifecycleStatus.Gone);
    }

    [Fact]
    public async Task GetStatusAsync_NonGuidThreadId_ReturnsUnclaimed()
    {
        var repository = new Mock<IAIConversationRepository>(MockBehavior.Strict);
        var provider = new ConversationFileThreadLifecycleProvider(repository.Object);

        var status = await provider.GetStatusAsync("not-a-conversation-id");

        status.ShouldBe(AIFileThreadLifecycleStatus.Unclaimed);
    }
}
