using Umbraco.Abilities.ChangeSets;
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
        ChangeSets = new ChangeSetService(new InMemoryChangeSetStore(), Abilities, Definitions, ItemTypes);
        Invoker = new AbilityInvoker(Abilities, ChangeSets);

        // Framework: the generic entity and change set abilities.
        Abilities
            .Add(new GetEntitySchemaAbility(Definitions, ItemTypes))
            .Add(new FindEntityAbility(Definitions, ChangeSets))
            .Add(new GetEntityAbility(Definitions, ChangeSets))
            .Add(new CreateEntityAbility())
            .Add(new SetEntityValueAbility())
            .Add(new AddEntityItemAbility())
            .Add(new RemoveEntityItemAbility())
            .Add(new MoveEntityItemAbility())
            .Add(new CreateChangeSetAbility(ChangeSets))
            .Add(new ValidateChangeSetAbility(ChangeSets))
            .Add(new SummarizeChangeSetAbility(ChangeSets))
            .Add(new ApplyChangeSetAbility(ChangeSets))
            .Add(new DiscardChangeSetAbility(ChangeSets));

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

    public ChangeSetService ChangeSets { get; }

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
