using Umbraco.Cms.Core.Notifications;
using Umbraco.Deploy.Core;
using Umbraco.Deploy.Core.Connectors.ServiceConnectors;
using Umbraco.Deploy.Core.NotificationHandlers;
using Umbraco.Deploy.Infrastructure.Disk;

namespace Umbraco.AI.Deploy.NotificationHandlers;

/// <summary>
/// Base class for handling saved notifications for Umbraco.AI entities.
/// Automatically writes artifacts to disk when entities are saved.
/// </summary>
public abstract class UmbracoAIEntitySavedDeployRefresherNotificationAsyncHandlerBase<TEntity, TNotification>
    : SavedDeployRefresherNotificationAsyncHandlerBase<TEntity, TNotification, TEntity>
    where TEntity : class
    where TNotification : SavedNotification<TEntity>
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

    protected override async Task HandleAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken)
    {
        // Get artifacts for saved entities (reverse to get latest unique entity)
        var artifacts = await _serviceConnectorFactory
            .GetArtifactsAsync(_entityType, entities.Reverse().DistinctBy(GetEntityId), new DictionaryCache(), cancellationToken)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Write to disk
        await _diskEntityService.WriteArtifactsAsync(artifacts).ConfigureAwait(false);

        // Update signatures
        _signatureService.SetSignatures(artifacts);
    }

    protected override IEnumerable<TEntity> GetEntities(IEnumerable<TNotification> notifications)
        => notifications.SelectMany(x => x.SavedEntities);

    protected abstract object GetEntityId(TEntity entity);
}
