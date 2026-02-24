using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Core.EntityAdapter.Adapters;

/// <summary>
/// Adapter for Umbraco CMS member entities.
/// Delegates formatting to the shared CMS property-based logic.
/// Provides member type sub-types.
/// </summary>
internal sealed class MemberEntityAdapter : AIEntityAdapterBase
{
    private readonly IMemberTypeService _memberTypeService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemberEntityAdapter"/> class.
    /// </summary>
    public MemberEntityAdapter(IMemberTypeService memberTypeService)
    {
        _memberTypeService = memberTypeService;
    }

    /// <inheritdoc />
    public override string? EntityType => "member";

    /// <inheritdoc />
    public override string Name => "Member";

    /// <inheritdoc />
    public override string? Icon => "icon-user";

    /// <inheritdoc />
    public override bool HasSubTypes => true;

    /// <inheritdoc />
    public override string FormatForLlm(AISerializedEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return CmsEntityFormatHelper.FormatCmsEntity(entity);
    }

    /// <inheritdoc />
    public override Task<IEnumerable<AIEntitySubType>> GetEntitySubTypesAsync(CancellationToken cancellationToken = default)
    {
        var memberTypes = _memberTypeService.GetAll()
            .Where(x => !x.IsElement)
            .Select(mt => new AIEntitySubType
            {
                Alias = mt.Alias,
                Name = mt.Name ?? mt.Alias,
                Icon = mt.Icon,
                Description = mt.Description,
                Unique = mt.Key.ToString()
            })
            .OrderBy(mt => mt.Name);

        return Task.FromResult<IEnumerable<AIEntitySubType>>(memberTypes);
    }
}
