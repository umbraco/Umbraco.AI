using Umbraco.Cms.Core.Composing;

namespace Umbraco.AI.Core.RuntimeContext;

/// <summary>
/// An ordered collection builder for AI runtime context contributors.
/// </summary>
/// <remarks>
/// <para>
/// Use this builder to configure the order of context contribution:
/// </para>
/// <code>
/// builder.AIRuntimeContextContributors()
///     .Append&lt;SerializedEntityContributor&gt;()
///     .Append&lt;DefaultSystemMessageContributor&gt;();
/// </code>
/// <para>
/// Contributors are executed in collection order for each context item.
/// Multiple contributors can handle the same item if their <c>CanHandle</c> returns true.
/// </para>
/// </remarks>
public sealed class AIRuntimeContextContributorCollectionBuilder
    : OrderedCollectionBuilderBase<AIRuntimeContextContributorCollectionBuilder, AIRuntimeContextContributorCollection, IAIRuntimeContextContributor>
{
    /// <inheritdoc />
    protected override AIRuntimeContextContributorCollectionBuilder This => this;
}
