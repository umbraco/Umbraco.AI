using Umbraco.Cms.Core.Composing;

namespace Umbraco.Ai.Core.Versioning;

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
/// builder.AiVersionableEntityAdapters()
///     .Add&lt;PromptVersionableEntityAdapter&gt;();
/// </code>
/// </remarks>
public class AiVersionableEntityAdapterCollectionBuilder
    : LazyCollectionBuilderBase<AiVersionableEntityAdapterCollectionBuilder, AiVersionableEntityAdapterCollection, IAiVersionableEntityAdapter>
{
    /// <inheritdoc />
    protected override AiVersionableEntityAdapterCollectionBuilder This => this;
}
