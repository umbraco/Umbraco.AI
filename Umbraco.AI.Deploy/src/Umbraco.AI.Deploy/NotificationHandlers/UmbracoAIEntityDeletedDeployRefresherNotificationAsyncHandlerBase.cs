using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Deploy.Core;
using Umbraco.Deploy.Infrastructure.Disk;

namespace Umbraco.AI.Deploy.NotificationHandlers;

/// <summary>
/// Base class for handling deleted notifications for Umbraco.AI entities.
/// Automatically removes artifacts from disk when entities are deleted.
/// NOTE: Umbraco.AI deleted notifications only provide EntityId, not the full entity.
/// </summary>
public abstract class UmbracoAIEntityDeletedDeployRefresherNotificationAsyncHandlerBase<TEntity, TNotification>
    : INotificationAsyncHandler<TNotification>
    where TEntity : class
    where TNotification : AIEntityDeletedNotification<TEntity>
{
    private readonly IDiskEntityService _diskEntityService;
    private readonly ISignatureService _signatureService;
    private readonly string _entityType;

    /// <summary>
    /// Initializes a new instance of the <see cref="UmbracoAIEntityDeletedDeployRefresherNotificationAsyncHandlerBase{TEntity, TNotification}"/> class.
    /// </summary>
    /// <param name="diskEntityService"></param>
    /// <param name="signatureService"></param>
    /// <param name="entityType"></param>
    protected UmbracoAIEntityDeletedDeployRefresherNotificationAsyncHandlerBase(
        IDiskEntityService diskEntityService,
        ISignatureService signatureService,
        string entityType)
    {
        _diskEntityService = diskEntityService;
        _signatureService = signatureService;
        _entityType = entityType;

        // Register entity type for disk-based deployment
        diskEntityService.RegisterDiskEntityType(entityType);
    }

    /// <inheritdoc />
    public Task HandleAsync(TNotification notification, CancellationToken cancellationToken)
    {
        var entityId = notification.EntityId;
        var udi = Udi.Create(_entityType, entityId);

        // Delete artifact from disk (using a placeholder name since we don't have the full entity)
        _diskEntityService.DeleteArtifacts(
            [entityId],
            id => Udi.Create(_entityType, id),
            id => $"Deleted {_entityType}"); // Placeholder name

        // Clear signature
        _signatureService.ClearSignatures([udi]);

        return Task.CompletedTask;
    }
}
