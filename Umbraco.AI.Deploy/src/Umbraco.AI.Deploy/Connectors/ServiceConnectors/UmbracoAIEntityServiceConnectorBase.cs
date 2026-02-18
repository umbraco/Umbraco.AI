using System.Runtime.CompilerServices;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Deploy.Configuration;
using Umbraco.AI.Deploy.Extensions;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Umbraco.Deploy.Infrastructure.Artifacts;
using Umbraco.Deploy.Infrastructure.Connectors.ServiceConnectors;

namespace Umbraco.AI.Deploy.Connectors.ServiceConnectors;

/// <summary>
/// Base class for Umbraco.AI entity service connectors.
/// Provides common functionality for deploying AI entities (Connections, Profiles, Contexts, Settings).
/// </summary>
public abstract class UmbracoAIEntityServiceConnectorBase<TArtifact, TEntity>(
    UmbracoAIDeploySettingsAccessor settingsAccessor)
    : ServiceConnectorBase<TArtifact, GuidUdi, TEntity>
    where TArtifact : DeployArtifactBase<GuidUdi>
    where TEntity : IAIEntity
{
    /// <summary>
    /// Accessor for retrieving Umbraco.AI deployment settings, allowing connectors to adapt behavior based on configuration.
    /// </summary>
    protected readonly UmbracoAIDeploySettingsAccessor _settingsAccessor = settingsAccessor;

    /// <summary>
    /// The entity type associated with this connector, used for constructing UDIs and ensuring correct entity handling.
    /// </summary>
    public abstract string UdiEntityType { get; }

    /// <summary>
    /// The name used for open UDIs (representing all entities of this type), typically in the format "All{EntityType}s" (e.g., "AllConnections").
    /// </summary>
    public virtual string ContainerId => "-1";

    /// <summary>
    /// The name used for open UDIs, typically in the format "All {EntityType}s" (e.g., "All Connections").
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public abstract string GetEntityName(TEntity entity);

    /// <summary>
    /// Retrieves an entity by its unique identifier (GUID). This method is used to fetch the current state of an entity during deployment operations.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public abstract Task<TEntity?> GetEntityAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all entities of the relevant type. This method is used to support operations that need to consider all existing entities, such as expanding open UDI ranges.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public abstract IAsyncEnumerable<TEntity> GetEntitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Constructs a deploy artifact for the specified entity. This method is responsible for translating an entity into its corresponding artifact representation, which can then be deployed to another environment.
    /// </summary>
    /// <param name="udi"></param>
    /// <param name="entity"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public abstract Task<TArtifact?> GetArtifactAsync(GuidUdi udi, TEntity? entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a deploy artifact for the specified entity. This method is called by the deployment framework when it needs to obtain the artifact representation of an entity, typically during export or when processing deployment operations.
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="contextCache"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override Task<TArtifact> GetArtifactAsync(
        TEntity entity,
        IContextCache contextCache,
        CancellationToken cancellationToken = default)
        => GetArtifactAsync(entity.GetUdi(UdiEntityType), entity, cancellationToken)!;

    /// <summary>
    /// Retrieves a deploy artifact for the specified entity identifier (UDI). This method is called by the deployment framework when it needs to obtain the artifact representation of an entity based on its identifier, typically during export or when processing deployment operations. The method first ensures that the UDI is of the correct type, then retrieves the corresponding entity and constructs the artifact from it.
    /// </summary>
    /// <param name="udi"></param>
    /// <param name="contextCache"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task<TArtifact?> GetArtifactAsync(
        GuidUdi udi,
        IContextCache contextCache,
        CancellationToken cancellationToken = default)
    {
        EnsureType(udi);
        TEntity? entity = await GetEntityAsync(udi.Guid, cancellationToken).ConfigureAwait(false);
        return entity == null ? null : await GetArtifactAsync(udi, entity, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves a UDI range for the specified entity identifier (UDI) and selector. This method is called by the deployment framework when it needs to determine the range of entities that should be included in a deployment operation based on a given UDI and selector. The method first ensures that the UDI is of the correct type, then retrieves the corresponding entity (if it exists) and constructs a UDI range that represents either the specific entity or all entities of that type, depending on whether the UDI is a root UDI or not.
    /// </summary>
    /// <param name="udi"></param>
    /// <param name="selector"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public override async Task<NamedUdiRange> GetRangeAsync(
        GuidUdi udi,
        string selector,
        CancellationToken cancellationToken = default)
    {
        EnsureType(udi);

        if (udi.IsRoot)
        {
            EnsureSelector(udi, selector);
            return new NamedUdiRange(udi, OpenUdiName, selector);
        }

        TEntity? entity = await GetEntityAsync(udi.Guid, cancellationToken).ConfigureAwait(false);

        if (entity == null)
        {
            throw new ArgumentException("Could not find an entity with the specified identifier.", nameof(udi));
        }

        return GetRange(entity, selector);
    }

    /// <summary>
    /// Retrieves a UDI range for the specified entity type, identifier (SID), and selector. This method is called by the deployment framework when it needs to determine the range of entities that should be included in a deployment operation based on a given entity type, identifier, and selector. The method first checks if the SID represents an open range (e.g., "-1" or the container ID), in which case it constructs a UDI range that represents all entities of that type. If the SID is not an open range, it attempts to parse it as a GUID, retrieves the corresponding entity, and constructs a UDI range for that specific entity.
    /// </summary>
    /// <param name="entityType"></param>
    /// <param name="sid"></param>
    /// <param name="selector"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public override async Task<NamedUdiRange> GetRangeAsync(
        string entityType,
        string sid,
        string selector,
        CancellationToken cancellationToken = default)
    {
        if (sid == "-1" || sid == ContainerId)
        {
            EnsureOpenSelector(selector);
            return new NamedUdiRange(Udi.Create(UdiEntityType), OpenUdiName, selector);
        }

        if (!Guid.TryParse(sid, out Guid result))
        {
            throw new ArgumentException("Invalid identifier.", nameof(sid));
        }

        TEntity? entity = await GetEntityAsync(result, cancellationToken).ConfigureAwait(false);

        if (entity == null)
        {
            throw new ArgumentException("Could not find an entity with the specified identifier.", nameof(sid));
        }

        return GetRange(entity, selector);
    }

    private NamedUdiRange GetRange(TEntity e, string selector)
        => new(e.GetUdi(UdiEntityType), GetEntityName(e), selector);

    /// <summary>
    /// Expands a UDI range into its individual UDIs. This method is called by the deployment framework when it needs to determine the specific entities that should be included in a deployment operation based on a given UDI range. The method first ensures that the UDI in the range is of the correct type, then checks if it is a root UDI (representing an open range). If it is a root UDI, it retrieves all entities of that type and yields their UDIs. If it is not a root UDI, it retrieves the specific entity corresponding to the UDI and yields its UDI if it exists.
    /// </summary>
    /// <param name="range"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public override async IAsyncEnumerable<GuidUdi> ExpandRangeAsync(
        UdiRange range,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureType(range.Udi);

        if (range.Udi.IsRoot)
        {
            EnsureSelector(range.Udi, range.Selector);

            await foreach (TEntity entity in GetEntitiesAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return entity.GetUdi(UdiEntityType);
            }
        }
        else
        {
            TEntity? entity = await GetEntityAsync(((GuidUdi)range.Udi).Guid, cancellationToken).ConfigureAwait(false);

            if (entity == null)
            {
                yield break;
            }

            if (range.Selector != "this")
            {
                throw new NotSupportedException("Unexpected selector \"" + range.Selector + "\".");
            }

            yield return entity.GetUdi(UdiEntityType);
        }
    }

    /// <summary>
    /// Processes the initialization of a deployment operation for a given artifact. This method is called by the deployment framework when it needs to determine the initial state of an entity during a deployment operation, typically when processing an artifact for the first time. The method first ensures that the UDI in the artifact is of the correct type, then retrieves the corresponding entity (if it exists) and creates an initial deployment state that includes both the artifact and the entity, along with the appropriate processing pass.
    /// </summary>
    /// <param name="artifact"></param>
    /// <param name="context"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task<ArtifactDeployState<TArtifact, TEntity>> ProcessInitAsync(
        TArtifact artifact,
        IDeployContext context,
        CancellationToken cancellationToken = default)
    {
        EnsureType(artifact.Udi);

        TEntity? entity = await GetEntityAsync(artifact.Udi.Guid, cancellationToken).ConfigureAwait(false);

        return ArtifactDeployState.Create(artifact, entity, this, ProcessPasses[0]);
    }
}
