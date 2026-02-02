using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.Versioning;

/// <summary>
/// A collection builder for versionable entity adapters.
/// </summary>
/// <remarks>
/// <para>
/// Use this builder to register entity-specific versioning adapters. Core adapters (Connection, Profile, Context)
/// are registered automatically. Add-on packages can register their own adapters:
/// </para>
/// <code>
/// // In a Composer
/// builder.AIVersionableEntityAdapters()
///     .Add&lt;PromptVersionableEntityAdapter&gt;();
/// </code>
/// </remarks>
public class AIVersionableEntityAdapterCollectionBuilder
    : LazyCollectionBuilderBase<AIVersionableEntityAdapterCollectionBuilder, AIVersionableEntityAdapterCollection, IAIVersionableEntityAdapter>
{
    /// <inheritdoc />
    protected override AIVersionableEntityAdapterCollectionBuilder This => this;
}
