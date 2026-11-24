// GP-2 (agents), GP-7 (AC4), GP-8 (AC4, AC8), GP-9 (AC2-AC5), GP-10 (AC2), GP-14 (AC3, AC4), GP-18 (AC5, AC6, AC8)

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using Moq;

using Shouldly;

using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Core.Security;

using Xunit;

using CmsSecurity = Umbraco.Cms.Core.Constants.Security;

namespace Umbraco.AI.Agent.Tests.Integration.Api;

/// <summary>
/// Real entry point (bdd-specs rule 4): the agent Management API over HTTP, through routing, binding, the real
/// AI policies, the real service and a real SQLite database. See <see cref="AgentApiTestHost"/> for what is
/// stood in for.
/// </summary>
public class AgentEndpointAccessTests : IAsyncLifetime
{
    private const string Agents = "/umbraco/ai/management/api/v1/agents";
    private static readonly Guid Editors = AgentApiTestHost.Editors;
    private static readonly Guid Translators = AgentApiTestHost.Translators;

    private AgentApiTestHost _host = null!;

    // A manager who is in the group the limited agent is limited to.
    private HttpClient _editorsManager = null!;

    // A manager who is not in it.
    private HttpClient _translatorsManager = null!;

    // Someone with the AI section and no AI rights, not in it.
    private HttpClient _translator = null!;

    // The admin.
    private HttpClient _admin = null!;

    public async Task InitializeAsync()
    {
        _host = await AgentApiTestHost.StartAsync();
        _editorsManager = _host.AddUser("editors-manager", [Editors], [AIPermissions.ManageAgents]);
        _translatorsManager = _host.AddUser("translators-manager", [Translators], [AIPermissions.ManageAgents]);
        _translator = _host.AddUser("translator", [Translators], []);
        _admin = _host.AddUser("admin", [CmsSecurity.AdminGroupKey], [AIPermissions.ManageAgents]);
    }

    public async Task DisposeAsync() => await _host.DisposeAsync();

    #region Happy path

    [Fact]
    public async Task PutAgent_WithAllowedUserGroupIds_RoundTrips()
    {
        // Given an AI section user with Uai.Agent.Manage
        var agent = await SeedAsync("round-trip", []);

        // When  they PUT agents/{alias} with allowedUserGroupIds [editors key]
        var put = await _admin.PutAsync($"{Agents}/round-trip", Json(UpdateBody("round-trip", allowedUserGroupIds: [Editors])));
        put.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Then  GET agents/{alias} returns allowedUserGroupIds [editors key] (GP-7 AC4)
        var detail = await ReadJsonAsync(await _admin.GetAsync($"{Agents}/round-trip"));
        detail["allowedUserGroupIds"]!.AsArray().Select(n => n!.GetValue<string>()).ShouldBe([Editors.ToString("D")]);
        (await _host.Repository.GetByIdAsync(agent)).ShouldNotBeNull().AllowedUserGroupIds.ShouldBe([Editors]);
    }

    [Fact]
    public async Task GetAgents_IncludeRestricted_ForManager_ListsLimitedAgent()
    {
        // Given a user with Uai.Agent.Manage not in editors
        await SeedAsync("limited", [Editors]);

        // When  they GET agents?includeRestricted=true
        var listed = await ListAliasesAsync(_translatorsManager, "?includeRestricted=true");

        // Then  the agent limited to editors is listed (GP-9 AC2)
        listed.ShouldBe(["limited"]);
    }

    [Fact]
    public async Task GetAgentDetail_ForManagerOutsideTheGroups_ReturnsTheLimitedAgent()
    {
        // Given a user with Uai.Agent.Manage not in editors, and an agent limited to editors
        await SeedAsync("limited", [Editors]);

        // When  they GET agents/{alias}
        var response = await _translatorsManager.GetAsync($"{Agents}/limited");

        // Then  they get it, so they can edit it (R12)
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PutAgent_ByManagerOutsideTheGroups_WithTheListUnchanged_Returns200()
    {
        // Given a manager not in editors, and an agent limited to editors
        await SeedAsync("limited", [Editors]);

        // When  they PUT agents/{alias} with the same list and a new name
        var response = await _translatorsManager.PutAsync(
            $"{Agents}/limited", Json(UpdateBody("limited", name: "Renamed", allowedUserGroupIds: [Editors])));

        // Then  it saves, because only changing the list is refused (R12)
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateAgent_WithManageAgents_Returns201()
    {
        // Given an AI section user with Uai.Agent.Manage
        // When  they POST agents with a valid body
        var response = await _editorsManager.PostAsync(Agents, Json(CreateBody("created")));

        // Then  the response is 201 (GP-2)
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetAgents_OnBehalfOfAccessibleWorkspace_ListsTheServiceAccountsAgents()
    {
        // Given a caller in translators and an agent limited to editors, the group of the workspace's service account
        await SeedAsync("limited", [Editors]);

        // When  they GET agents?onBehalfOf=automate-workspace:accessible
        var listed = await ListAliasesAsync(_translator, $"?onBehalfOf={AgentApiTestHost.WorkspaceKind}:{AgentApiTestHost.AccessibleWorkspace}");

        // Then  the agent is listed, because the service account can use it (GP-14)
        listed.ShouldBe(["limited"]);
    }

    [Fact]
    public async Task GetAgentDetail_OnBehalfOfAnAccountThatCannotUseIt_Returns404()
    {
        // Given an agent limited to translators and a workspace whose service account is in editors
        await SeedAsync("translators-only", [Translators]);

        // When  the caller (a translators manager who could use it) asks for it on behalf of the workspace
        var response = await _translatorsManager.GetAsync(
            $"{Agents}/translators-only?onBehalfOf={AgentApiTestHost.WorkspaceKind}:{AgentApiTestHost.AccessibleWorkspace}");

        // Then  it is not found, as the service account sees it, not the caller
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAgentDetail_OnBehalfOfAnAccountThatCanUseIt_Returns200()
    {
        // Given an agent limited to editors, the group of the workspace's service account, and a caller outside it
        await SeedAsync("editors-only", [Editors]);

        // When  the caller asks for it on behalf of the workspace
        var response = await _translator.GetAsync(
            $"{Agents}/editors-only?onBehalfOf={AgentApiTestHost.WorkspaceKind}:{AgentApiTestHost.AccessibleWorkspace}");

        // Then  they get it, because the service account can use it (GP-14)
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PutAgent_WithoutAllowedUserGroupIds_KeepsTheStoredList()
    {
        // Given an agent limited to editors
        var id = await SeedAsync("limited", [Editors]);

        // When  an admin PUTs it without allowedUserGroupIds
        var response = await _admin.PutAsync(
            $"{Agents}/limited", Json(UpdateBody("limited", name: "Renamed", includeAllowedUserGroupIds: false)));

        // Then  it saves and the list is unchanged
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var stored = (await _host.Repository.GetByIdAsync(id)).ShouldNotBeNull();
        stored.Name.ShouldBe("Renamed");
        stored.AllowedUserGroupIds.ShouldBe([Editors]);
    }

    [Fact]
    public async Task PutAgent_WithAnEmptyAllowedUserGroupIds_OpensTheAgentToEveryone()
    {
        // Given an agent limited to editors
        var id = await SeedAsync("limited", [Editors]);

        // When  an admin PUTs it with allowedUserGroupIds []
        var response = await _admin.PutAsync($"{Agents}/limited", Json(UpdateBody("limited", allowedUserGroupIds: [])));

        // Then  the list is cleared
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await _host.Repository.GetByIdAsync(id)).ShouldNotBeNull().AllowedUserGroupIds.ShouldBeEmpty();
    }

    #endregion

    #region Sad path

    [Fact]
    public async Task CreateAgent_WithoutManageAgents_Returns403()
    {
        // Given an AI section user without Uai.Agent.Manage
        // When  they POST agents
        var response = await _translator.PostAsync(Agents, Json(CreateBody("created")));

        // Then  the response is 403 (GP-2)
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await _host.Repository.GetByAliasAsync("created")).ShouldBeNull();
    }

    [Fact]
    public async Task CreateAgent_WithTheRightButNotTheAISection_Returns403()
    {
        // Given a user with Uai.Agent.Manage but without the AI section
        var client = _host.AddUser("no-section", [Editors], [AIPermissions.ManageAgents], hasAISection: false);

        // When  they POST agents
        var response = await client.PostAsync(Agents, Json(CreateBody("created")));

        // Then  the response is 403
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteAgent_WithoutManageAgents_Returns403AndKeepsTheAgent()
    {
        // Given an AI section user without Uai.Agent.Manage and an existing agent
        var id = await SeedAsync("keep", []);

        // When  they DELETE agents/{alias}
        var response = await _translator.DeleteAsync($"{Agents}/keep");

        // Then  the response is 403 and the agent is still there (GP-2)
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await _host.Repository.GetByIdAsync(id)).ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteAgent_ByManagerOutsideTheGroups_Returns204()
    {
        // Given a manager not in editors, and an agent limited to editors
        await SeedAsync("limited", [Editors]);

        // When  they DELETE agents/{alias}
        var response = await _translatorsManager.DeleteAsync($"{Agents}/limited");

        // Then  it is deleted: managing agents doesn't need being able to use them (R12)
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task PutAgent_AddingSomeoneElsesGroup_Returns403()
    {
        // Given a non-admin with Uai.Agent.Manage in editors only
        var id = await SeedAsync("limited", [Editors]);

        // When  they PUT agents/{alias} adding translators
        var response = await _editorsManager.PutAsync(
            $"{Agents}/limited", Json(UpdateBody("limited", name: "Changed", allowedUserGroupIds: [Editors, Translators])));

        // Then  the response is 403 (GP-8 AC4) and nothing is saved
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var stored = (await _host.Repository.GetByIdAsync(id))!;
        stored.AllowedUserGroupIds.ShouldBe([Editors]);
        stored.Name.ShouldBe("Probe");
    }

    [Fact]
    public async Task GetAgents_ForNonMember_HidesLimitedAgent()
    {
        // Given an agent limited to editors and a user not in it
        await SeedAsync("limited", [Editors]);
        await SeedAsync("open", []);

        // When  they GET agents
        var listed = await ListAliasesAsync(_translator, "");

        // Then  the agent isn't listed (GP-9 AC3)
        listed.ShouldBe(["open"]);
    }

    [Fact]
    public async Task GetAgents_IncludeRestricted_WithoutManage_IsIgnored()
    {
        // Given a user without Uai.Agent.Manage and not in editors
        await SeedAsync("limited", [Editors]);

        // When  they GET agents?includeRestricted=true
        var listed = await ListAliasesAsync(_translator, "?includeRestricted=true");

        // Then  the limited agent isn't listed (GP-9 AC4)
        listed.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAgentDetail_ForNonMemberWithoutManage_Returns404()
    {
        // Given a user without Uai.Agent.Manage and not in editors
        await SeedAsync("limited", [Editors]);

        // When  they GET agents/{alias} for the limited agent
        var response = await _translator.GetAsync($"{Agents}/limited");

        // Then  the response is 404 (GP-9 AC5)
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    public static TheoryData<string, string, string?> RunEndpoints => new()
    {
        { "POST", "run", """{ "messages": [ { "role": "user", "content": "hi" } ] }""" },
        { "POST", "stream", """{ "messages": [ { "role": "user", "content": "hi" } ] }""" },
        { "POST", "stream-agui", """{ "threadId": "t", "runId": "r", "messages": [] }""" },
        { "POST", "suggest-starters", null },
        { "GET", "suggest-starters/availability", null },
    };

    [Theory]
    [MemberData(nameof(RunEndpoints))]
    public async Task RunEndpoints_ForNonMember_Return403AgentNotAvailable(string method, string path, string? body)
    {
        // Given an agent limited to editors and a user not in it
        var id = await SeedAsync("limited", [Editors]);

        foreach (var idOrAlias in new[] { "limited", id.ToString() })
        {
            // When  they call run, stream, stream-agui, suggest-starters or its availability, by alias and by id
            var response = await SendAsync(_translator, method, $"{Agents}/{idOrAlias}/{path}", body);

            // Then  the response is 403 ProblemDetails titled "Agent not available" (GP-10 AC2), not a started stream
            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden, $"{method} {path} by {idOrAlias}");
            (await ReadJsonAsync(response))["title"]!.GetValue<string>().ShouldBe("Agent not available");
        }
    }

    [Theory]
    [MemberData(nameof(RunEndpoints))]
    public async Task RunEndpoints_ForAnUnknownAlias_Return404(string method, string path, string? body)
    {
        // Given no agent with the alias
        // When  a user calls the endpoint
        var response = await SendAsync(_translator, method, $"{Agents}/no-such-agent/{path}", body);

        // Then  it is 404, so the 403 above can't be mistaken for a lookup that hides limited agents
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PutAgent_ChangingOtherGroupsToolRules_Returns403()
    {
        // Given a non-admin in editors only and an agent with tool rules for translators
        var id = await SeedAsync("limited", [Editors], toolRules: new() { [Translators] = ["tool-a"] });

        // When  they PUT agents/{alias} with changed translators rules
        var response = await _editorsManager.PutAsync(
            $"{Agents}/limited", Json(UpdateBody("limited", allowedUserGroupIds: [Editors], toolRules: new() { [Translators] = ["tool-b"] })));

        // Then  the response is 403 (GP-18 AC5) and the rules are unchanged
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        ToolRulesOf(await _host.Repository.GetByIdAsync(id)).ShouldBe(new() { [Translators] = "tool-a" });
    }

    [Fact]
    public async Task PutAgent_RemovingOtherGroupsToolRules_Returns403()
    {
        // Given the same non-admin and agent
        var id = await SeedAsync("limited", [Editors], toolRules: new() { [Translators] = ["tool-a"] });

        // When  they PUT agents/{alias} without the translators entry
        var response = await _editorsManager.PutAsync(
            $"{Agents}/limited", Json(UpdateBody("limited", allowedUserGroupIds: [Editors], toolRules: [])));

        // Then  the response is 403 (GP-18 AC6) and the rules are unchanged
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        ToolRulesOf(await _host.Repository.GetByIdAsync(id)).ShouldBe(new() { [Translators] = "tool-a" });
    }

    [Fact]
    public async Task Rollback_ThatChangesTheGroupList_Returns403()
    {
        // Given a non-admin in editors only, an agent limited to editors, and an old version limited to editors and translators
        var id = await SeedAsync("limited", [Editors]);
        SetSnapshot(id, allowedUserGroupIds: [Editors, Translators]);

        // When  they roll the agent back to that version
        var response = await _editorsManager.PostAsync($"/umbraco/ai/management/api/v1/versions/agent/{id}/1/rollback", null);

        // Then  the response is 403 (GP-8 AC8) and nothing is restored
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await _host.Repository.GetByIdAsync(id))!.AllowedUserGroupIds.ShouldBe([Editors]);
    }

    [Fact]
    public async Task Rollback_ThatChangesAnotherGroupsToolRules_Returns403()
    {
        // Given a non-admin in editors only, an agent with tool rules for translators, and an old version with other ones
        var id = await SeedAsync("limited", [Editors], toolRules: new() { [Translators] = ["tool-a"] });
        SetSnapshot(id, allowedUserGroupIds: [Editors], toolRules: new() { [Translators] = ["tool-b"] });

        // When  they roll the agent back to that version
        var response = await _editorsManager.PostAsync($"/umbraco/ai/management/api/v1/versions/agent/{id}/1/rollback", null);

        // Then  the response is 403 (GP-18 AC8) and the rules are unchanged
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        ToolRulesOf(await _host.Repository.GetByIdAsync(id)).ShouldBe(new() { [Translators] = "tool-a" });
    }

    [Fact]
    public async Task GetAgents_OnBehalfOfInaccessibleWorkspace_Returns403()
    {
        // Given a caller without access to workspace W
        // When  they GET agents?onBehalfOf=automate-workspace:W
        var response = await _translator.GetAsync(
            $"{Agents}?onBehalfOf={AgentApiTestHost.WorkspaceKind}:{AgentApiTestHost.InaccessibleWorkspace}");

        // Then  the response is 403 (GP-14 AC3)
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("unknown:1")]
    [InlineData("automate-workspace:no-service-account")]
    [InlineData("not-a-kind-and-value")]
    public async Task GetAgents_OnBehalfOfUnknownKindOrNoServiceAccount_Returns400(string onBehalfOf)
    {
        // Given onBehalfOf=unknown:1, or a workspace with no service account, or no kind at all
        // When  GET agents is called with it
        var response = await _translator.GetAsync($"{Agents}?onBehalfOf={Uri.EscapeDataString(onBehalfOf)}");

        // Then  the response is 400 (GP-14 AC4)
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAgentDetail_OnBehalfOfInaccessibleWorkspace_Returns403()
    {
        // Given an agent and a workspace the caller has no access to
        await SeedAsync("any", []);

        // When  the caller GETs agents/{alias}?onBehalfOf=automate-workspace:W
        var response = await _translator.GetAsync(
            $"{Agents}/any?onBehalfOf={AgentApiTestHost.WorkspaceKind}:{AgentApiTestHost.InaccessibleWorkspace}");

        // Then  the response is 403 (GP-14 AC3)
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("unknown:1")]
    [InlineData("automate-workspace:no-service-account")]
    public async Task GetAgentDetail_OnBehalfOfUnknownKindOrNoServiceAccount_Returns400(string onBehalfOf)
    {
        // Given an agent
        await SeedAsync("any", []);

        // When  the caller GETs agents/{alias}?onBehalfOf= an unknown kind, or a workspace with no service account
        var response = await _translator.GetAsync($"{Agents}/any?onBehalfOf={Uri.EscapeDataString(onBehalfOf)}");

        // Then  the response is 400 (GP-14 AC4)
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Helpers

    private async Task<Guid> SeedAsync(string alias, Guid[] allowedUserGroupIds, Dictionary<Guid, string[]>? toolRules = null)
    {
        var agent = new AIAgent
        {
            Id = Guid.NewGuid(),
            Alias = alias,
            Name = "Probe",
            ProfileId = Guid.NewGuid(),
            AllowedUserGroupIds = allowedUserGroupIds,
            Config = new AIStandardAgentConfig { UserGroupPermissions = ToPermissions(toolRules) },
        };

        await _host.Repository.SaveAsync(agent);
        return agent.Id;
    }

    private void SetSnapshot(Guid id, Guid[] allowedUserGroupIds, Dictionary<Guid, string[]>? toolRules = null)
        => _host.Versions
            .Setup(v => v.GetVersionSnapshotAsync<AIAgent>(id, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIAgent
            {
                Id = id,
                Alias = "limited",
                Name = "Probe",
                ProfileId = Guid.NewGuid(),
                AllowedUserGroupIds = allowedUserGroupIds,
                Config = new AIStandardAgentConfig { UserGroupPermissions = ToPermissions(toolRules) },
            });

    private static Dictionary<Guid, AIAgentUserGroupPermissions> ToPermissions(Dictionary<Guid, string[]>? toolRules)
        => (toolRules ?? []).ToDictionary(
            kvp => kvp.Key,
            kvp => new AIAgentUserGroupPermissions { AllowedToolIds = kvp.Value });

    // Tool ids joined into one string per group, so the dictionaries compare by value.
    private static Dictionary<Guid, string> ToolRulesOf(AIAgent? agent)
        => ((AIStandardAgentConfig)agent!.Config!).UserGroupPermissions
            .ToDictionary(kvp => kvp.Key, kvp => string.Join(",", kvp.Value.AllowedToolIds));

    private static string CreateBody(string alias)
        => new JsonObject { ["alias"] = alias, ["name"] = "Created", ["agentType"] = "standard" }.ToJsonString();

    private static string UpdateBody(
        string alias,
        string name = "Probe",
        Guid[]? allowedUserGroupIds = null,
        Dictionary<Guid, string[]>? toolRules = null,
        bool includeAllowedUserGroupIds = true)
    {
        var rules = new JsonObject();
        foreach (var (group, tools) in toolRules ?? [])
        {
            rules[group.ToString("D")] = new JsonObject { ["allowedToolIds"] = new JsonArray(tools.Select(t => (JsonNode)t).ToArray()) };
        }

        var body = new JsonObject
        {
            ["alias"] = alias,
            ["name"] = name,
            ["isActive"] = true,
            ["config"] = new JsonObject { ["$type"] = "standard", ["userGroupPermissions"] = rules },
        };

        if (includeAllowedUserGroupIds)
        {
            body["allowedUserGroupIds"] = new JsonArray((allowedUserGroupIds ?? []).Select(g => (JsonNode)g.ToString("D")).ToArray());
        }

        return body.ToJsonString();
    }

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");

    private static async Task<JsonNode> ReadJsonAsync(HttpResponseMessage response)
        => JsonNode.Parse(await response.Content.ReadAsStringAsync())!;

    private static async Task<string[]> ListAliasesAsync(HttpClient client, string query)
    {
        var response = await client.GetAsync($"{Agents}{query}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ReadJsonAsync(response);
        return body["items"]!.AsArray().Select(i => i!["alias"]!.GetValue<string>()).ToArray();
    }

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, string method, string url, string? body)
        => client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = body is null ? null : Json(body),
        });

    #endregion
}
