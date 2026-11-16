// S3 - Rewrite stored messages in one step (AC1-AC3)
using NUnit.Framework;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Integration;

/// <summary>
/// Feature: the conversation store rewrites messages (updates and deletes) in one transaction.
/// Pending until T1 adds IAIConversationRepository.RewriteMessagesAsync.
/// </summary>
[TestFixture]
internal sealed class RewriteMessagesTests
{
    private const string Pending = "Pending: T1";

    // Scenario: a rewrite succeeds (happy path)

    // AC1 - Given a conversation with stored messages; when a rewrite updates one message's content,
    // then that message holds the new content and keeps its Sequence and DateCreated
    [Test]
    [Ignore(Pending)]
    public void Rewrite_WithAnUpdate_SavesTheNewContentAndKeepsSequenceAndDateCreated()
        => Assert.Fail(Pending);

    // AC2 - Given a conversation with stored messages; when a rewrite deletes two messages by id,
    // then those messages are gone and the rest keep their order
    [Test]
    [Ignore(Pending)]
    public void Rewrite_WithTwoDeletes_RemovesThoseMessagesAndKeepsTheRestInOrder()
        => Assert.Fail(Pending);

    // Scenario: sad path

    // AC3 - Given a rewrite with one update and one delete; when the delete fails,
    // then the update is not saved either
    [Test]
    [Ignore(Pending)]
    public void Rewrite_WhenTheDeleteFails_DoesNotSaveTheUpdate()
        => Assert.Fail(Pending);
}
