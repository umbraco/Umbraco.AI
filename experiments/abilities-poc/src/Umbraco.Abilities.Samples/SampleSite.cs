using Umbraco.Abilities.ChangeSets;
using Umbraco.Abilities.Entities;
using Umbraco.Abilities.Samples.Acme;
using Umbraco.Abilities.Samples.Automate;
using Umbraco.Abilities.Samples.Automate.Plugins;
using Umbraco.Abilities.Samples.Cms;
using Umbraco.Abilities.Samples.Commerce;
using Umbraco.Abilities.Samples.Commerce.Domain;
using Umbraco.Abilities.Samples.Email;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.HostedServices;

namespace Umbraco.Abilities.Samples;

/// <summary>
/// Wires the samples together the way composers would in a real site. Each block below is what one
/// package would register; none of them refers to an AI, Automate or MCP product.
/// </summary>
public sealed class SampleSite
{
    private SampleSite(IBackgroundTaskQueue? background, IEventAggregator? events)
    {
        ChangeSets = new ChangeSetService(new InMemoryChangeSetStore(), Abilities, Definitions, ItemTypes, events, background, TimeSpan.Zero);
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
            .Add(new DiscardChangeSetAbility(ChangeSets))
            .Add(new ResolveConflictAbility(ChangeSets));

        // CMS content.
        Pages = new CmsPageDefinition(ContentDatabase, ItemTypes);
        Media = new CmsMediaDefinition(ContentDatabase, ItemTypes);
        Definitions.Add(Pages).Add(Media);
        CmsPageDefinition.RegisterItemTypes(ItemTypes);
        CmsMediaDefinition.RegisterItemTypes(ItemTypes);

        // Commerce.
        Collections = new CollectionDefinition(CommerceDatabase, ItemTypes);
        DiscountService = new DiscountService(CommerceDatabase);
        Discounts = new DiscountDefinition(DiscountService, RuleProviders, RewardProviders, CommerceDatabase, ItemTypes);
        Definitions.Add(Collections).Add(Discounts);
        RuleProviders.AddRange([new OrderTotalRuleProvider(), new CustomerGroupRuleProvider(), new ProductCollectionRuleProvider(), new GroupRuleProvider()]);
        RewardProviders.AddRange([new PercentageOffRewardProvider(), new FreeShippingRewardProvider()]);
        DiscountDefinition.RegisterItemTypes(ItemTypes, RuleProviders, RewardProviders, user => Access.HasSection(user, "commerce"));
        var activateDiscount = new ActivateDiscountAbility(Discounts);
        Abilities.Add(activateDiscount);
        SummerCollection = Collections.Seed("Summer collection");
        Collections.Seed("Winter coats");

        // A third-party discount rule: added to Commerce's provider collection, as its composer would. No Abilities code.
        RuleProviders.Add(new AcmeDayOfWeekRuleProvider());

        // An email package.
        var sendEmail = new SendEmailAbility(Outbox);
        Abilities.Add(sendEmail);

        // Automate: its step types are other packages' abilities.
        Workflows = new WorkflowDefinition(AutomateDatabase);
        Definitions.Add(Workflows);
        Triggers.AddRange([typeof(FormSubmittedTrigger), typeof(ScheduleTrigger)]);
        WorkflowDefinition.RegisterItemTypes(ItemTypes, [sendEmail, activateDiscount], Triggers, Actions, Access.HasSection);

        // Commerce's Automate package adds its trigger and action, as its composer would. No Abilities code.
        Triggers.Add(typeof(OrderCreatedTrigger));
        Actions.Add(typeof(ChangeOrderStatusAction));

        // Each product checks access with its own permission model.
        Pages.Access = Media.Access = Access;
        Collections.Access = Discounts.Access = Access;
        Workflows.Access = Access;
    }

    public SampleAccess Access { get; } = new();

    /// <summary>Commerce's discount rule and reward provider collections, which packages add to.</summary>
    public DiscountProviderCollection RuleProviders { get; } = [];

    public DiscountProviderCollection RewardProviders { get; } = [];

    /// <summary>Automate's trigger and action collections, which packages add to.</summary>
    public AutomatePluginCollection Triggers { get; } = [];

    public AutomatePluginCollection Actions { get; } = [];

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

    public DiscountService DiscountService { get; }

    public DiscountDefinition Discounts { get; }

    public WorkflowDefinition Workflows { get; }

    public Guid SummerCollection { get; }

    public static SampleSite Create() => new(null, null);

    /// <summary>A site whose side effects with retries run on a background queue, with notifications.</summary>
    public static SampleSite Create(IBackgroundTaskQueue background, IEventAggregator events) => new(background, events);
}
