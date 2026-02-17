using Umbraco.AI.Core.Models.Notifications;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Deploy.Core;
using Umbraco.Deploy.Core.Connectors.ServiceConnectors;
using Umbraco.Deploy.Infrastructure.Disk;

namespace Umbraco.AI.Deploy.NotificationHandlers;

/// <summary>
/// Base class for handling saved notifications for Umbraco.AI entities.
/// Automatically writes artifacts to disk when entities are saved.
/// NOTE: Umbraco.AI uses AIEntitySavedNotification with singular Entity property,
/// not Umbraco's standard SavedNotification with SavedEntities collection.
/// </summary>
public abstract class UmbracoAIEntitySavedDeployRefresherNotificationAsyncHandlerBase<TEntity, TNotification>
    : INotificationAsyncHandler<TNotification>
    where TEntity : class
    where TNotification : AIEntitySavedNotification<TEntity>
{
    private readonly IServiceConnectorFactory _serviceConnectorFactory;
    private readonly IDiskEntityService _diskEntityService;
    private readonly ISignatureService _signatureService;
    private readonly string _entityType;

    protected UmbracoAIEntitySavedDeployRefresherNotificationAsyncHandlerBase(
        IServiceConnectorFactory serviceConnectorFactory,
        IDiskEntityService diskEntityService,
        ISignatureService signatureService,
        string entityType)
    {
        _serviceConnectorFactory = serviceConnectorFactory;
        _diskEntityService = diskEntityService;
        _signatureService = signatureService;
        _entityType = entityType;

        // Register entity type for disk-based deployment
        diskEntityService.RegisterDiskEntityType(entityType);
    }

    public async Task HandleAsync(TNotification notification, CancellationToken cancellationToken)
    {
        var entity = notification.Entity;

        // Get artifact for saved entity
        var artifacts = await _serviceConnectorFactory
            .GetArtifactsAsync(_entityType, new[] { entity }, new DictionaryCache(), cancellationToken)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Write to disk
        await _diskEntityService.WriteArtifactsAsync(artifacts).ConfigureAwait(false);

        // Update signatures
        _signatureService.SetSignatures(artifacts);
    }

    protected abstract object GetEntityId(TEntity entity);
}
