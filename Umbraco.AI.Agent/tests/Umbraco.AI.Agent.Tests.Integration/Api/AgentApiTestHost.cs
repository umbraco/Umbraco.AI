using System.Security.Claims;
using System.Security.Principal;
using System.Text.Encodings.Web;

using Asp.Versioning;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

using OpenIddict.Validation.AspNetCore;

using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Persistence;
using Umbraco.AI.Agent.Persistence.Agents;
using Umbraco.AI.Agent.Tests.Integration.Fixtures;
using Umbraco.AI.Agent.Web.Api.Management.Agent.Controllers;
using Umbraco.AI.Agent.Web.Api.Management.Agent.Mapping;
using Umbraco.AI.Core.Configuration;
using Umbraco.AI.Core.Security;
using Umbraco.AI.Core.Versioning;
using Umbraco.AI.Web.Api.Management.Versioning.Controllers;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Api.Common.DependencyInjection;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Security.Authorization;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Web.Common.Routing;

using AIConstants = Umbraco.AI.Core.Constants;
using CmsConstants = Umbraco.Cms.Core.Constants;

namespace Umbraco.AI.Agent.Tests.Integration.Api;

/// <summary>
/// The agent Management API controllers, served over HTTP with everything between the request and the
/// service real: routing and API versioning, model binding and JSON, the real AI authorization policies and
/// handlers, the real mapper definitions, the real permission service, the real <see cref="AIAgentService"/> and
/// its real SQLite repository. Only the backoffice login, which needs the whole CMS host, is replaced by a
/// header naming one of the users registered with <see cref="AddUser"/>.
/// </summary>
/// <remarks>
/// The CMS's <c>UmbracoTestServerTestBase</c> would also serve the login, but it is NUnit-only and boots
/// every composer of the CMS and of both packages, which this project's xUnit tests cannot host.
/// </remarks>
internal sealed class AgentApiTestHost : IAsyncDisposable
{
    public static readonly Guid Editors = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid Translators = Guid.Parse("22222222-2222-2222-2222-222222222222");

    /// <summary>The <c>onBehalfOf</c> kind the test resolver claims.</summary>
    public const string WorkspaceKind = "automate-workspace";

    /// <summary>A workspace the caller may act for; its service account is in <see cref="Editors"/>.</summary>
    public const string AccessibleWorkspace = "accessible";

    /// <summary>A workspace the caller may not act for.</summary>
    public const string InaccessibleWorkspace = "inaccessible";

    /// <summary>A workspace with no service account.</summary>
    public const string WorkspaceWithoutServiceAccount = "no-service-account";

    private const string UserHeader = "X-Test-User";

    private readonly SqliteConnection _connection;
    private readonly Dictionary<string, IUser> _users;
    private readonly WebApplication _app;

    private AgentApiTestHost(
        SqliteConnection connection,
        WebApplication app,
        EFCoreAIAgentRepository repository,
        Mock<IAIEntityVersionService> versions,
        Dictionary<string, IUser> users)
    {
        _connection = connection;
        _app = app;
        Repository = repository;
        Versions = versions;
        _users = users;
    }

    /// <summary>The real repository behind the service, to seed agents and to read back what was saved.</summary>
    public EFCoreAIAgentRepository Repository { get; }

    /// <summary>The version history service, mocked: set up a snapshot here to roll an agent back to.</summary>
    public Mock<IAIEntityVersionService> Versions { get; }

    public static async Task<AgentApiTestHost> StartAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        UmbracoAIAgentDbContext CreateContext()
            => new(new DbContextOptionsBuilder<UmbracoAIAgentDbContext>()
                .UseSqlite(connection, x =>
                {
                    x.MigrationsAssembly("Umbraco.AI.Agent.Persistence.Sqlite");
                    x.MigrationsHistoryTable(AIConnectionStringResolver.MigrationsHistoryTableName);
                })
                .Options);

        await using (var db = CreateContext())
        {
            await db.Database.MigrateAsync();
        }

        var repository = new EFCoreAIAgentRepository(new TestEFCoreScopeProvider(CreateContext));
        var users = new Dictionary<string, IUser>();
        var versions = new Mock<IAIEntityVersionService>();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        ConfigureServices(builder.Services, repository, versions.Object, users);

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync();

        // The users dictionary is shared with the services, so users added later are seen by them too.
        return new AgentApiTestHost(connection, app, repository, versions, users);
    }

    /// <summary>
    /// Registers a backoffice user and returns a client that is logged in as them.
    /// </summary>
    /// <param name="name">A name for the user.</param>
    /// <param name="groupKeys">The user groups they belong to. <see cref="CmsConstants.Security.AdminGroupKey"/> makes them an admin.</param>
    /// <param name="verbs">The AI rights every one of their groups holds.</param>
    /// <param name="hasAISection">Whether their groups give access to the AI section.</param>
    public HttpClient AddUser(string name, Guid[] groupKeys, string[] verbs, bool hasAISection = true)
    {
        var groups = groupKeys.Select(key =>
        {
            var group = new Mock<IReadOnlyUserGroup>();
            group.SetupGet(g => g.Key).Returns(key);
            group.SetupGet(g => g.Alias).Returns(key.ToString());
            group.SetupGet(g => g.Permissions).Returns(verbs.ToHashSet());
            return group.Object;
        }).ToArray();

        var user = new Mock<IUser>();
        user.SetupGet(u => u.Key).Returns(Guid.NewGuid());
        user.SetupGet(u => u.Groups).Returns(groups);
        user.SetupGet(u => u.AllowedSections).Returns(hasAISection ? [AIConstants.Sections.AI] : []);
        _users[name] = user.Object;

        var client = _app.GetTestClient();
        client.DefaultRequestHeaders.Add(UserHeader, name);
        return client;
    }

    /// <summary>A client with no login.</summary>
    public HttpClient CreateAnonymousClient() => _app.GetTestClient();

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private static void ConfigureServices(
        IServiceCollection services,
        EFCoreAIAgentRepository repository,
        IAIEntityVersionService versions,
        Dictionary<string, IUser> users)
    {
        services.AddLogging();
        services.AddHttpContextAccessor();

        // Routing: the controllers' real attributes, with the CMS's own backoffice token replaced the way the CMS does.
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        }).AddMvc();

        var controllerTypes = new[]
        {
            typeof(AllAgentController),
            typeof(ByIdOrAliasAgentController),
            typeof(CreateAgentController),
            typeof(UpdateAgentController),
            typeof(DeleteAgentController),
            typeof(RunAgentController),
            typeof(StreamAgentController),
            typeof(StreamAgentAGUIController),
            typeof(SuggestStartersAgentController),
            typeof(EntityVersionHistoryController),
        };

        var mvc = services.AddControllers(options =>
            options.Conventions.Add(new UmbracoBackofficeToken(CmsConstants.Web.AttributeRouting.BackOfficeToken, "umbraco")));
        mvc.PartManager.ApplicationParts.Add(new ControllerTypesPart(controllerTypes));
        mvc.AddJsonOptions(Umbraco.AI.Web.Constants.ManagementApi.ApiName, _ => { });
        mvc.AddJsonOptions(Umbraco.AI.Agent.Web.Constants.ManagementApi.ApiName, _ => { });

        // Authentication: the header names the user. Authorization: the real AI policies, handlers and permission service.
        services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>(
                OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, _ => { });
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.BackOfficeAccess, policy => policy.RequireAuthenticatedUser());
            AIAuthorizationPolicies.Register(options);
        });
        services.AddSingleton<IAuthorizationHandler, AISectionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, AIPermissionAuthorizationHandler>();

        services.AddSingleton<IAuthorizationHelper>(sp => new HeaderAuthorizationHelper(users));
        services.AddSingleton<IBackOfficeSecurityAccessor>(sp => new HeaderBackOfficeSecurityAccessor(
            sp.GetRequiredService<IHttpContextAccessor>(), users));
        services.AddSingleton<IAIPermissionService, AIPermissionService>();

        // The agent service, real, over the real repository. What a refused request never reaches is mocked.
        services.AddSingleton<IAIAgentRepository>(repository);
        services.AddSingleton(versions);
        services.AddSingleton(Mock.Of<Umbraco.AI.Agent.Core.Chat.IAIAgentFactory>());
        services.AddSingleton(Mock.Of<Umbraco.AI.Agent.Core.AGUI.IAGUIStreamingService>());
        services.AddSingleton(Mock.Of<Umbraco.AI.Agent.Core.AGUI.IAGUIContextConverter>());
        services.AddSingleton(Mock.Of<Umbraco.AI.Agent.Core.AGUI.IAGUIMessageConverter>());
        services.AddSingleton(new Umbraco.AI.Core.Tools.AIToolCollection(() => []));
        services.AddSingleton(Mock.Of<Umbraco.AI.Core.Profiles.IAIProfileService>());
        services.AddSingleton(Mock.Of<Umbraco.AI.Core.Guardrails.IAIGuardrailService>());
        services.AddSingleton(Mock.Of<Umbraco.AI.Core.Contexts.IAIContextService>());
        services.AddSingleton(Mock.Of<Umbraco.Cms.Core.Events.IEventAggregator>());
        services.AddSingleton<AIAgentService>();
        services.AddSingleton<IAIAgentService>(sp => sp.GetRequiredService<AIAgentService>());

        // Controller dependencies other than the service.
        services.AddSingleton(Mock.Of<IAIStarterPromptSuggester>());
        services.AddSingleton(Mock.Of<Umbraco.AI.Agent.Core.Agents.Selection.IAIAgentSelectionService>());
        services.AddSingleton(Mock.Of<Umbraco.AI.Agent.Core.AGUI.IAGUIToolConverter>());
        services.AddSingleton(Mock.Of<Umbraco.AI.Core.RuntimeContext.IAIRuntimeContextScopeProvider>());
        services.AddSingleton(new Umbraco.AI.Core.RuntimeContext.AIRuntimeContextContributorCollection(() => []));
        services.AddSingleton<AIAgentScopeValidator>();
        services.AddSingleton(new Umbraco.AI.Agent.Core.Surfaces.AIAgentSurfaceCollection(() => []));

        // onBehalfOf: the real dispatcher over one test resolver.
        services.AddSingleton(new AIAgentUserGroupResolverCollection(() => [new WorkspaceResolver()]));
        services.AddSingleton<IAIAgentUserGroupResolutionService, AIAgentUserGroupResolutionService>();

        // Rollback: the core controller over the real agent adapter.
        services.AddSingleton<IAIVersionableEntityAdapter>(sp => new AIAgentVersionableEntityAdapter(
            sp.GetRequiredService<IAIAgentService>(), sp.GetRequiredService<IAIEntityVersionService>()));
        services.AddSingleton(sp => new AIVersionableEntityAdapterCollection(
            () => sp.GetServices<IAIVersionableEntityAdapter>()));

        services.AddSingleton<IUmbracoMapper>(sp => new UmbracoMapper(
            new MapDefinitionCollection(() => new IMapDefinition[]
            {
                new AgentMapDefinition(Mock.Of<IShortStringHelper>()),
                new Umbraco.AI.Web.Api.Management.Common.Mapping.CommonMapDefinition(),
            }),
            Mock.Of<ICoreScopeProvider>(),
            NullLogger<UmbracoMapper>.Instance));
    }

    private static IUser UserOf(IPrincipal? principal, Dictionary<string, IUser> users)
        => principal?.Identity?.Name is { } name && users.TryGetValue(name, out var user)
            ? user
            : throw new InvalidOperationException("No test user for this request.");

    private sealed class ControllerTypesPart(Type[] types) : ApplicationPart, IApplicationPartTypeProvider
    {
        public override string Name => "agent-api-test-controllers";

        public IEnumerable<System.Reflection.TypeInfo> Types => types.Select(t => System.Reflection.IntrospectionExtensions.GetTypeInfo(t));
    }

    private sealed class HeaderAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(UserHeader, out var name))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, name.ToString())], Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }

    private sealed class HeaderAuthorizationHelper(Dictionary<string, IUser> users) : IAuthorizationHelper
    {
        public IUser GetUmbracoUser(IPrincipal currentUser) => UserOf(currentUser, users);
    }

    private sealed class HeaderBackOfficeSecurityAccessor(IHttpContextAccessor httpContextAccessor, Dictionary<string, IUser> users)
        : IBackOfficeSecurityAccessor
    {
        public IBackOfficeSecurity? BackOfficeSecurity
        {
            get
            {
                var principal = httpContextAccessor.HttpContext?.User;
                if (principal?.Identity?.Name is not { } name || !users.TryGetValue(name, out var user))
                {
                    return null;
                }

                var security = new Mock<IBackOfficeSecurity>();
                security.SetupGet(s => s.CurrentUser).Returns(user);
                return security.Object;
            }
        }
    }

    /// <summary>
    /// Stands in for Automate's resolver: one kind, three workspaces, one for each way the real one answers.
    /// </summary>
    private sealed class WorkspaceResolver : IAIAgentUserGroupResolver
    {
        public string Kind => WorkspaceKind;

        public Task<Attempt<IReadOnlyList<Guid>, AIAgentUserGroupResolutionStatus>> ResolveUserGroupsAsync(
            string value,
            CancellationToken cancellationToken = default)
            => Task.FromResult(value switch
            {
                AccessibleWorkspace => Attempt.SucceedWithStatus<IReadOnlyList<Guid>, AIAgentUserGroupResolutionStatus>(
                    AIAgentUserGroupResolutionStatus.Success, [Editors]),
                WorkspaceWithoutServiceAccount => Attempt.FailWithStatus<IReadOnlyList<Guid>, AIAgentUserGroupResolutionStatus>(
                    AIAgentUserGroupResolutionStatus.NoUser, []),
                _ => Attempt.FailWithStatus<IReadOnlyList<Guid>, AIAgentUserGroupResolutionStatus>(
                    AIAgentUserGroupResolutionStatus.Forbidden, []),
            });
    }
}
