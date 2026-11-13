using Umbraco.Abilities.DraftSets;
using Umbraco.Abilities.Entities;
using Umbraco.Abilities.Samples.Acme;
using Umbraco.Abilities.Samples.Automate;
using Umbraco.Abilities.Samples.Cms;
using Umbraco.Abilities.Samples.Commerce;
using Umbraco.Abilities.Samples.Email;

namespace Umbraco.Abilities.Samples;

/// <summary>
/// Wires the samples together the way composers would in a real site. Each block below is what one
/// package would register; none of them refers to an AI, Automate or MCP product.
/// </summary>
public sealed class SampleSite
{
    private SampleSite()
    {
        DraftSets = new DraftSetService(new InMemoryDraftSetStore(), Abilities, Definitions, ItemTypes);
        Invoker = new AbilityInvoker(Abilities, DraftSets);

        // Framework: the generic entity and draft set abilities.
        Abilities
            .Add(new GetEntitySchemaAbility(Definitions, ItemTypes))
            .Add(new FindEntityAbility(Definitions, DraftSets))
            .Add(new GetEntityAbility(Definitions, DraftSets))
            .Add(new CreateEntityAbility())
            .Add(new SetEntityValueAbility())
            .Add(new AddEntityItemAbility())
            .Add(new RemoveEntityItemAbility())
            .Add(new MoveEntityItemAbility())
            .Add(new CreateDraftSetAbility(DraftSets))
            .Add(new ValidateDraftSetAbility(DraftSets))
            .Add(new SummarizeDraftSetAbility(DraftSets))
            .Add(new ApplyDraftSetAbility(DraftSets))
            .Add(new DiscardDraftSetAbility(DraftSets));

        // CMS content.
        Pages = new CmsPageDefinition(ContentDatabase);
        Media = new CmsMediaDefinition(ContentDatabase);
        Definitions.Add(Pages).Add(Media);
        CmsPageDefinition.RegisterItemTypes(ItemTypes);

        // Commerce.
        Collections = new CollectionDefinition(CommerceDatabase);
        Discounts = new DiscountDefinition(CommerceDatabase);
        Definitions.Add(Collections).Add(Discounts);
        DiscountDefinition.RegisterItemTypes(ItemTypes);
        var activateDiscount = new ActivateDiscountAbility(Discounts);
        Abilities.Add(activateDiscount);
        SummerCollection = Collections.Seed("Summer collection");
        Collections.Seed("Winter coats");

        // A third-party discount rule.
        AcmeWeekdayRule.Register(ItemTypes);

        // An email package.
        var sendEmail = new SendEmailAbility(Outbox);
        Abilities.Add(sendEmail);

        // Automate: its step types are other packages' abilities.
        Workflows = new WorkflowDefinition(AutomateDatabase);
        Definitions.Add(Workflows);
        WorkflowDefinition.RegisterItemTypes(ItemTypes, [sendEmail, activateDiscount]);
    }

    // Separate databases, as the products can each have their own connection string.
    public InMemoryDatabase ContentDatabase { get; } = new("Content");

    public InMemoryDatabase CommerceDatabase { get; } = new("Commerce");

    public InMemoryDatabase AutomateDatabase { get; } = new("Automate");

    public AbilityRegistry Abilities { get; } = new();

    public EntityDefinitionRegistry Definitions { get; } = new();

    public ItemTypeRegistry ItemTypes { get; } = new();

    public DraftSetService DraftSets { get; }

    public AbilityInvoker Invoker { get; }

    public Outbox Outbox { get; } = new();

    public CmsPageDefinition Pages { get; }

    public CmsMediaDefinition Media { get; }

    public CollectionDefinition Collections { get; }

    public DiscountDefinition Discounts { get; }

    public WorkflowDefinition Workflows { get; }

    public Guid SummerCollection { get; }

    public static SampleSite Create() => new();
}
