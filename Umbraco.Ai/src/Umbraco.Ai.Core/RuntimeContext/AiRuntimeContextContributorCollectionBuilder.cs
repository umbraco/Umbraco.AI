using Umbraco.Cms.Core.Composing;

namespace Umbraco.Ai.Core.RuntimeContext;

/// <summary>
/// An ordered collection builder for AI runtime context contributors.
/// </summary>
/// <remarks>
/// <para>
/// Use this builder to configure the order of context contribution:
/// </para>
/// <code>
/// builder.AiRuntimeContextContributors()
///     .Append&lt;SerializedEntityContributor&gt;()
///     .Append&lt;DefaultSystemMessageContributor&gt;();
/// </code>
/// <para>
/// Contributors are executed in collection order for each context item.
/// Multiple contributors can handle the same item if their <c>CanHandle</c> returns true.
/// </para>
/// </remarks>
public sealed class AiRuntimeContextContributorCollectionBuilder
    : OrderedCollectionBuilderBase<AiRuntimeContextContributorCollectionBuilder, AiRuntimeContextContributorCollection, IAiRuntimeContextContributor>
{
    /// <inheritdoc />
    protected override AiRuntimeContextContributorCollectionBuilder This => this;
}
