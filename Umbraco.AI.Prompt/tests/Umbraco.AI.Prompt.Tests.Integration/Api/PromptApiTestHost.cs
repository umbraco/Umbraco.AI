using System.Security.Claims;
using System.Security.Principal;
using System.Text.Encodings.Web;

using Asp.Versioning;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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

using Umbraco.AI.Core.Configuration;
using Umbraco.AI.Core.Security;
using Umbraco.AI.Core.Versioning;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.AI.Prompt.Persistence;
using Umbraco.AI.Prompt.Persistence.Prompts;
using Umbraco.AI.Prompt.Tests.Integration.Fixtures;
using Umbraco.AI.Prompt.Web.Api.Management.Prompt.Controllers;
using Umbraco.AI.Prompt.Web.Api.Management.Prompt.Mapping;
using Umbraco.AI.Prompt.Web.Authorization;
using Umbraco.AI.Web.Api.Management.Versioning.Controllers;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Api.Common.DependencyInjection;
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

namespace Umbraco.AI.Prompt.Tests.Integration.Api;

/// <summary>
/// The prompt Management API controllers, served over HTTP with everything between the request and the
/// service real: routing and API versioning, model binding and JSON, the real AI authorization policies and
/// handlers, the real mapper definitions, the real permission service, the real <see cref="AIPromptService"/> and
/// its real SQLite repository. Only the backoffice login, which needs the whole CMS host, is replaced by a
/// header naming one of the users registered with <see cref="AddUser"/>.
/// </summary>
/// <remarks>
/// Mirrors the agent test host. The CMS's <c>UmbracoTestServerTestBase</c> is NUnit-only and boots every
/// composer, which this project's xUnit tests cannot host.
/// </remarks>
internal sealed class PromptApiTestHost : IAsyncDisposable
{
    public static readonly Guid Editors = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid Translators = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private const string UserHeader = "X-Test-User";

    private readonly SqliteConnection _connection;
    private readonly Dictionary<string, IUser> _users;
    private readonly WebApplication _app;

    private PromptApiTestHost(
        SqliteConnection connection,
        WebApplication app,
        EFCoreAIPromptRepository repository,
        Mock<IAIEntityVersionService> versions,
        Dictionary<string, IUser> users)
    {
        _connection = connection;
        _app = app;
        Repository = repository;
        Versions = versions;
        _users = users;
    }

    /// <summary>The real repository behind the service, to seed prompts and to read back what was saved.</summary>
    public EFCoreAIPromptRepository Repository { get; }

    /// <summary>The version history service, mocked: set up a snapshot here to roll a prompt back to.</summary>
    public Mock<IAIEntityVersionService> Versions { get; }

    public static async Task<PromptApiTestHost> StartAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        UmbracoAIPromptDbContext CreateContext()
            => new(new DbContextOptionsBuilder<UmbracoAIPromptDbContext>()
                .UseSqlite(connection, x =>
                {
                    x.MigrationsAssembly("Umbraco.AI.Prompt.Persistence.Sqlite");
                    x.MigrationsHistoryTable(AIConnectionStringResolver.MigrationsHistoryTableName);
                })
                .Options);

        await using (var db = CreateContext())
        {
            await db.Database.MigrateAsync();
        }

        var repository = new EFCoreAIPromptRepository(new TestEFCoreScopeProvider(CreateContext));
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
        return new PromptApiTestHost(connection, app, repository, versions, users);
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
        EFCoreAIPromptRepository repository,
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
            typeof(AllPromptController),
            typeof(ByIdOrAliasPromptController),
            typeof(CreatePromptController),
            typeof(UpdatePromptController),
            typeof(DeletePromptController),
            typeof(ExecutePromptController),
            typeof(AliasExistsPromptController),
            typeof(EntityVersionHistoryController),
        };

        var mvc = services.AddControllers(options =>
            options.Conventions.Add(new UmbracoBackofficeToken(CmsConstants.Web.AttributeRouting.BackOfficeToken, "umbraco")));
        mvc.PartManager.ApplicationParts.Add(new ControllerTypesPart(controllerTypes));
        mvc.AddJsonOptions(Umbraco.AI.Web.Constants.ManagementApi.ApiName, _ => { });
        mvc.AddJsonOptions(Umbraco.AI.Prompt.Web.Constants.ManagementApi.ApiName, _ => { });

        // Authentication: the header names the user. Authorization: the real AI policies, handlers and permission service.
        // The AI policies name both the OpenIddict and the backoffice cookie scheme, as CMS 19's own do.
        services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>(
                OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme, _ => { })
            .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>(
                CmsConstants.Security.BackOfficeAuthenticationType, _ => { });
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.BackOfficeAccess, policy => policy.RequireAuthenticatedUser());
            AIAuthorizationPolicies.Register(options);
            AIPromptAuthorizationPolicies.Register(options);
        });
        services.AddSingleton<IAuthorizationHandler, AISectionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, AIPermissionAuthorizationHandler>();

        services.AddSingleton<IAuthorizationHelper>(sp => new HeaderAuthorizationHelper(users));
        services.AddSingleton<IBackOfficeSecurityAccessor>(sp => new HeaderBackOfficeSecurityAccessor(
            sp.GetRequiredService<IHttpContextAccessor>(), users));
        services.AddSingleton<IAIPermissionService, AIPermissionService>();

        // The prompt service, real, over the real repository. What a refused request never reaches is mocked.
        services.AddSingleton<IAIPromptRepository>(repository);
        services.AddSingleton(versions);
        services.AddSingleton(Mock.Of<Umbraco.AI.Core.Chat.IAIChatService>());
        services.AddSingleton(Mock.Of<IAIPromptTemplateService>());
        services.AddSingleton(Mock.Of<Umbraco.AI.Core.Tools.IAIFunctionFactory>());
        services.AddSingleton(new Umbraco.AI.Core.Tools.AIToolCollection(() => []));
        services.AddSingleton(Mock.Of<Umbraco.AI.Core.RuntimeContext.IAIRuntimeContextScopeProvider>());
        services.AddSingleton(new Umbraco.AI.Core.RuntimeContext.AIRuntimeContextContributorCollection(() => []));
        services.AddSingleton(Mock.Of<Umbraco.Cms.Core.Events.IEventAggregator>());
        services.AddSingleton(Mock.Of<IAIPromptPropertyValueSchemaResolver>());
        services.AddSingleton(new Umbraco.AI.Core.PropertyValueOperations.AISimplifiedPropertyValueTransformerCollection(() => []));
        services.AddSingleton<AIPromptService>();
        services.AddSingleton<IAIPromptService>(sp => sp.GetRequiredService<AIPromptService>());

        // Rollback: the core controller over the real prompt adapter.
        services.AddSingleton<IAIVersionableEntityAdapter>(sp => new AIPromptVersionableEntityAdapter(
            sp.GetRequiredService<IAIPromptService>(), sp.GetRequiredService<IAIEntityVersionService>()));
        services.AddSingleton(sp => new AIVersionableEntityAdapterCollection(
            () => sp.GetServices<IAIVersionableEntityAdapter>()));

        services.AddSingleton<IUmbracoMapper>(sp => new UmbracoMapper(
            new MapDefinitionCollection(() => new IMapDefinition[]
            {
                new PromptMapDefinition(Mock.Of<IShortStringHelper>()),
                new PromptExecutionMapDefinition(),
                new Umbraco.AI.Web.Api.Management.Common.Mapping.CommonMapDefinition(),
            }),
            TestCoreScopeProvider.Create(),
            NullLogger<UmbracoMapper>.Instance));
    }

    private static IUser UserOf(IPrincipal? principal, Dictionary<string, IUser> users)
        => principal?.Identity?.Name is { } name && users.TryGetValue(name, out var user)
            ? user
            : throw new InvalidOperationException("No test user for this request.");

    private sealed class ControllerTypesPart(Type[] types) : ApplicationPart, IApplicationPartTypeProvider
    {
        public override string Name => "prompt-api-test-controllers";

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
}
