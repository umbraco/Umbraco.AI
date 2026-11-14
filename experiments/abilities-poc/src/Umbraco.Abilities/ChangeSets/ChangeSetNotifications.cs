using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.Abilities.ChangeSets;

// CMS notifications, so packages handle change sets the way they handle content: a handler can veto applying a set
// as it can veto saving a document, and audit, caches or integrations can react once it is applied or discarded.

/// <summary>A change set is about to be applied (or to resume applying). Cancel it to refuse; nothing is applied.</summary>
public sealed class ChangeSetApplyingNotification(ChangeSet changeSet, EventMessages messages) : CancelableNotification(messages)
{
    public ChangeSet ChangeSet { get; } = changeSet;
}

/// <summary>Every step of a change set is applied: its changes are saved and its side effects have run.</summary>
public sealed class ChangeSetAppliedNotification(ChangeSet changeSet, IReadOnlyList<EntityRef> saved) : INotification
{
    public ChangeSet ChangeSet { get; } = changeSet;

    public IReadOnlyList<EntityRef> Saved { get; } = saved;
}

public sealed class ChangeSetDiscardedNotification(ChangeSet changeSet) : INotification
{
    public ChangeSet ChangeSet { get; } = changeSet;
}
