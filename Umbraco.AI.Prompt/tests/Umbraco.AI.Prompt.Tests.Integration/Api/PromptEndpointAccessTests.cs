// GP-2 (prompts), GP-12 (AC4, AC5), GP-13 (AC2-AC5)

using System.Net;
using System.Text;
using System.Text.Json.Nodes;

using Moq;

using Shouldly;

using Umbraco.AI.Core.Security;
using Umbraco.AI.Prompt.Core.Prompts;

using Xunit;

using CmsSecurity = Umbraco.Cms.Core.Constants.Security;

namespace Umbraco.AI.Prompt.Tests.Integration.Api;

/// <summary>
/// Real entry point (bdd-specs rule 4): the prompt Management API over HTTP, through routing, binding, the real
/// AI policies, the real service and a real SQLite database. See <see cref="PromptApiTestHost"/> for what is
/// stood in for.
/// </summary>
public class PromptEndpointAccessTests : IAsyncLifetime
{
    private const string Prompts = "/umbraco/ai/management/api/v1/prompts";
    private static readonly Guid Editors = PromptApiTestHost.Editors;
    private static readonly Guid Translators = PromptApiTestHost.Translators;

    private const string ExecuteBody =
        """{ "entityId": "33333333-3333-3333-3333-333333333333", "entityType": "document", "propertyAlias": "title", "contentTypeAlias": "page" }""";

    private PromptApiTestHost _host = null!;

    // A manager who is in the group the limited prompt is limited to.
    private HttpClient _editorsManager = null!;

    // A manager who is not in it.
    private HttpClient _translatorsManager = null!;

    // Someone with the AI section and no AI rights, not in it.
    private HttpClient _translator = null!;

    // The admin.
    private HttpClient _admin = null!;

    public async Task InitializeAsync()
    {
        _host = await PromptApiTestHost.StartAsync();
        _editorsManager = _host.AddUser("editors-manager", [Editors], [AIPermissions.ManagePrompts]);
        _translatorsManager = _host.AddUser("translators-manager", [Translators], [AIPermissions.ManagePrompts]);
        _translator = _host.AddUser("translator", [Translators], []);
        _admin = _host.AddUser("admin", [CmsSecurity.AdminGroupKey], [AIPermissions.ManagePrompts]);
    }

    public async Task DisposeAsync() => await _host.DisposeAsync();

    #region Happy path

    [Fact]
    public async Task CreatePrompt_WithManagePrompts_Returns201()
    {
        // Given an AI section user with Uai.Prompt.Manage
        // When  they POST prompts with a valid body
        var response = await _editorsManager.PostAsync(Prompts, Json(CreateBody("created")));

        // Then  the response is 201 (GP-2)
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreatePrompt_WithAllowedUserGroupIds_StoresThem()
    {
        // Given an admin
        // When  they POST prompts with allowedUserGroupIds [editors]
        var response = await _admin.PostAsync(Prompts, Json(CreateBody("created", [Editors])));

        // Then  the list is stored
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await _host.Repository.GetByAliasAsync("created")).ShouldNotBeNull().AllowedUserGroupIds.ShouldBe([Editors]);
    }

    [Fact]
    public async Task PutPrompt_WithAllowedUserGroupIds_RoundTrips()
    {
        // Given an AI section user with Uai.Prompt.Manage
        var id = await SeedAsync("round-trip", []);

        // When  they PUT prompts/{alias} with allowedUserGroupIds [editors key]
        var put = await _admin.PutAsync($"{Prompts}/round-trip", Json(UpdateBody("round-trip", allowedUserGroupIds: [Editors])));
        put.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Then  GET prompts/{alias} returns allowedUserGroupIds [editors key] (GP-12 AC2)
        var detail = await ReadJsonAsync(await _admin.GetAsync($"{Prompts}/round-trip"));
        detail["allowedUserGroupIds"]!.AsArray().Select(n => n!.GetValue<string>()).ShouldBe([Editors.ToString("D")]);
        (await _host.Repository.GetByIdAsync(id)).ShouldNotBeNull().AllowedUserGroupIds.ShouldBe([Editors]);
    }

    [Fact]
    public async Task GetPrompts_IncludeRestricted_ForManager_ListsLimitedPrompt()
    {
        // Given a user with Uai.Prompt.Manage not in editors
        await SeedAsync("limited", [Editors]);

        // When  they GET prompts?includeRestricted=true
        var listed = await ListAliasesAsync(_translatorsManager, "?includeRestricted=true");

        // Then  the prompt limited to editors is listed (GP-13 AC5)
        listed.ShouldBe(["limited"]);
    }

    [Fact]
    public async Task GetPrompts_ForMember_ListsLimitedPrompt()
    {
        // Given a prompt limited to editors and a user in editors
        await SeedAsync("limited", [Editors]);

        // When  they GET prompts
        var listed = await ListAliasesAsync(_editorsManager, "");

        // Then  the prompt is offered (GP-13 AC1)
        listed.ShouldBe(["limited"]);
    }

    [Fact]
    public async Task GetPromptDetail_ForManagerOutsideTheGroups_ReturnsTheLimitedPrompt()
    {
        // Given a user with Uai.Prompt.Manage not in editors, and a prompt limited to editors
        await SeedAsync("limited", [Editors]);

        // When  they GET prompts/{alias}
        var response = await _translatorsManager.GetAsync($"{Prompts}/limited");

        // Then  they get it, so they can edit it (R12)
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PutPrompt_ByManagerOutsideTheGroups_WithTheListUnchanged_Returns200()
    {
        // Given a manager not in editors, and a prompt limited to editors
        await SeedAsync("limited", [Editors]);

        // When  they PUT prompts/{alias} with the same list and a new name
        var response = await _translatorsManager.PutAsync(
            $"{Prompts}/limited", Json(UpdateBody("limited", name: "Renamed", allowedUserGroupIds: [Editors])));

        // Then  it saves, because only changing the list is refused (R12)
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PutPrompt_WithoutAllowedUserGroupIds_KeepsTheStoredList()
    {
        // Given a prompt limited to editors
        var id = await SeedAsync("limited", [Editors]);

        // When  an admin PUTs it without allowedUserGroupIds
        var response = await _admin.PutAsync(
            $"{Prompts}/limited", Json(UpdateBody("limited", name: "Renamed", includeAllowedUserGroupIds: false)));

        // Then  it saves and the list is unchanged
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var stored = (await _host.Repository.GetByIdAsync(id)).ShouldNotBeNull();
        stored.Name.ShouldBe("Renamed");
        stored.AllowedUserGroupIds.ShouldBe([Editors]);
    }

    [Fact]
    public async Task PutPrompt_WithAnEmptyAllowedUserGroupIds_OpensThePromptToEveryone()
    {
        // Given a prompt limited to editors
        var id = await SeedAsync("limited", [Editors]);

        // When  an admin PUTs it with allowedUserGroupIds []
        var response = await _admin.PutAsync($"{Prompts}/limited", Json(UpdateBody("limited", allowedUserGroupIds: [])));

        // Then  the list is cleared
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await _host.Repository.GetByIdAsync(id)).ShouldNotBeNull().AllowedUserGroupIds.ShouldBeEmpty();
    }

    #endregion

    #region Sad path

    [Fact]
    public async Task CreatePrompt_WithoutManagePrompts_Returns403()
    {
        // Given an AI section user without Uai.Prompt.Manage
        // When  they POST prompts
        var response = await _translator.PostAsync(Prompts, Json(CreateBody("created")));

        // Then  the response is 403 (GP-2)
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await _host.Repository.GetByAliasAsync("created")).ShouldBeNull();
    }

    [Fact]
    public async Task UpdatePrompt_WithoutManagePrompts_Returns403()
    {
        // Given an AI section user without Uai.Prompt.Manage and an open prompt
        var id = await SeedAsync("open", []);

        // When  they PUT prompts/{alias}
        var response = await _translator.PutAsync($"{Prompts}/open", Json(UpdateBody("open", name: "Changed")));

        // Then  the response is 403 and nothing is saved (GP-2)
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await _host.Repository.GetByIdAsync(id))!.Name.ShouldBe("Probe");
    }

    [Fact]
    public async Task DeletePrompt_WithoutManagePrompts_Returns403AndKeepsThePrompt()
    {
        // Given an AI section user without Uai.Prompt.Manage and an existing prompt
        var id = await SeedAsync("keep", []);

        // When  they DELETE prompts/{alias}
        var response = await _translator.DeleteAsync($"{Prompts}/keep");

        // Then  the response is 403 and the prompt is still there (GP-2)
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await _host.Repository.GetByIdAsync(id)).ShouldNotBeNull();
    }

    [Fact]
    public async Task CreatePrompt_WithTheRightButNotTheAISection_Returns403()
    {
        // Given a user with Uai.Prompt.Manage but without the AI section
        var client = _host.AddUser("no-section", [Editors], [AIPermissions.ManagePrompts], hasAISection: false);

        // When  they POST prompts
        var response = await client.PostAsync(Prompts, Json(CreateBody("created")));

        // Then  the response is 403
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeletePrompt_ByManagerOutsideTheGroups_Returns204()
    {
        // Given a manager not in editors, and a prompt limited to editors
        await SeedAsync("limited", [Editors]);

        // When  they DELETE prompts/{alias}
        var response = await _translatorsManager.DeleteAsync($"{Prompts}/limited");

        // Then  it is deleted: managing prompts doesn't need being able to use them (R12)
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task PutPrompt_AddingSomeoneElsesGroup_Returns403()
    {
        // Given a non-admin with Uai.Prompt.Manage in editors only
        var id = await SeedAsync("limited", [Editors]);

        // When  they PUT prompts/{alias} adding translators
        var response = await _editorsManager.PutAsync(
            $"{Prompts}/limited", Json(UpdateBody("limited", name: "Changed", allowedUserGroupIds: [Editors, Translators])));

        // Then  the response is 403 (GP-12 AC4) and nothing is saved
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        var stored = (await _host.Repository.GetByIdAsync(id))!;
        stored.AllowedUserGroupIds.ShouldBe([Editors]);
        stored.Name.ShouldBe("Probe");
    }

    [Fact]
    public async Task PutPrompt_ByANonAdminRemovingTheirOwnGroup_Returns403()
    {
        // Given a prompt limited to editors and a non-admin manager in editors
        var id = await SeedAsync("limited", [Editors]);

        // When  they remove editors and save, which would open the prompt to everyone
        var response = await _editorsManager.PutAsync(
            $"{Prompts}/limited", Json(UpdateBody("limited", allowedUserGroupIds: [])));

        // Then  the response is 403 (GP-12 AC5) and the list is unchanged
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await _host.Repository.GetByIdAsync(id))!.AllowedUserGroupIds.ShouldBe([Editors]);
    }

    [Fact]
    public async Task CreatePrompt_ByANonAdminWithAGroupTheyAreNotIn_Returns403()
    {
        // Given a non-admin manager in editors only
        // When  they POST prompts limited to translators
        var response = await _editorsManager.PostAsync(Prompts, Json(CreateBody("created", [Translators])));

        // Then  the response is 403 and nothing is created
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await _host.Repository.GetByAliasAsync("created")).ShouldBeNull();
    }

    [Fact]
    public async Task GetPrompts_ForNonMember_HidesLimitedPrompt()
    {
        // Given a prompt limited to editors and a user not in it
        await SeedAsync("limited", [Editors]);
        await SeedAsync("open", []);

        // When  they GET prompts
        var listed = await ListAliasesAsync(_translator, "");

        // Then  the prompt isn't listed (GP-13 AC2)
        listed.ShouldBe(["open"]);
    }

    [Fact]
    public async Task GetPrompts_IncludeRestricted_WithoutManage_IsIgnored()
    {
        // Given a user without Uai.Prompt.Manage and not in editors
        await SeedAsync("limited", [Editors]);

        // When  they GET prompts?includeRestricted=true
        var listed = await ListAliasesAsync(_translator, "?includeRestricted=true");

        // Then  the limited prompt isn't listed
        listed.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPromptDetail_ForNonMemberWithoutManage_Returns404()
    {
        // Given a user without Uai.Prompt.Manage and not in editors
        await SeedAsync("limited", [Editors]);

        // When  they GET prompts/{alias} for the limited prompt
        var response = await _translator.GetAsync($"{Prompts}/limited");

        // Then  the response is 404 (GP-13 AC3)
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ExecutePrompt_ForNonMember_Returns403PromptNotAvailable()
    {
        // Given a prompt limited to editors and a user not in it
        var id = await SeedAsync("limited", [Editors]);

        foreach (var idOrAlias in new[] { "limited", id.ToString() })
        {
            // When  they POST prompts/{idOrAlias}/execute, by alias and by id
            var response = await _translator.PostAsync($"{Prompts}/{idOrAlias}/execute", Json(ExecuteBody));

            // Then  the response is 403 ProblemDetails titled "Prompt not available" (GP-13 AC4)
            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden, idOrAlias);
            (await ReadJsonAsync(response))["title"]!.GetValue<string>().ShouldBe("Prompt not available");
        }
    }

    [Fact]
    public async Task ExecutePrompt_ForAManagerOutsideTheGroups_StillReturns403()
    {
        // Given a prompt limited to editors and a manager not in it
        await SeedAsync("limited", [Editors]);

        // When  they POST prompts/{alias}/execute
        var response = await _translatorsManager.PostAsync($"{Prompts}/limited/execute", Json(ExecuteBody));

        // Then  managing prompts doesn't let them run it
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("no-such-prompt")]
    [InlineData("44444444-4444-4444-4444-444444444444")]
    public async Task ExecutePrompt_ForAnUnknownPrompt_Returns404(string idOrAlias)
    {
        // Given no prompt with the alias or id
        // When  a user executes it
        var response = await _translator.PostAsync($"{Prompts}/{idOrAlias}/execute", Json(ExecuteBody));

        // Then  it is 404, so the 403 above can't be mistaken for a lookup that hides limited prompts
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Rollback_ThatChangesTheGroupList_Returns403()
    {
        // Given a non-admin in editors only, a prompt limited to editors, and an old version limited to editors and translators
        var id = await SeedAsync("limited", [Editors]);
        _host.Versions
            .Setup(v => v.GetVersionSnapshotAsync<AIPrompt>(id, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AIPrompt
            {
                Id = id,
                Alias = "limited",
                Name = "Probe",
                Instructions = "Do it",
                AllowedUserGroupIds = [Editors, Translators],
            });

        // When  they roll the prompt back to that version
        var response = await _editorsManager.PostAsync($"/umbraco/ai/management/api/v1/versions/prompt/{id}/1/rollback", null);

        // Then  the response is 403 and nothing is restored
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await _host.Repository.GetByIdAsync(id))!.AllowedUserGroupIds.ShouldBe([Editors]);
    }

    #endregion

    #region Helpers

    private async Task<Guid> SeedAsync(string alias, Guid[] allowedUserGroupIds)
    {
        var prompt = new AIPrompt
        {
            Id = Guid.NewGuid(),
            Alias = alias,
            Name = "Probe",
            Instructions = "Do it",
            AllowedUserGroupIds = allowedUserGroupIds,
        };

        await _host.Repository.SaveAsync(prompt);
        return prompt.Id;
    }

    private static string CreateBody(string alias, Guid[]? allowedUserGroupIds = null)
    {
        var body = new JsonObject { ["alias"] = alias, ["name"] = "Created", ["instructions"] = "Do it" };
        if (allowedUserGroupIds is not null)
        {
            body["allowedUserGroupIds"] = ToJson(allowedUserGroupIds);
        }

        return body.ToJsonString();
    }

    private static string UpdateBody(
        string alias,
        string name = "Probe",
        Guid[]? allowedUserGroupIds = null,
        bool includeAllowedUserGroupIds = true)
    {
        var body = new JsonObject
        {
            ["alias"] = alias,
            ["name"] = name,
            ["instructions"] = "Do it",
            ["isActive"] = true,
        };

        if (includeAllowedUserGroupIds)
        {
            body["allowedUserGroupIds"] = ToJson(allowedUserGroupIds ?? []);
        }

        return body.ToJsonString();
    }

    private static JsonArray ToJson(Guid[] groups) => new(groups.Select(g => (JsonNode)g.ToString("D")).ToArray());

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");

    private static async Task<JsonNode> ReadJsonAsync(HttpResponseMessage response)
        => JsonNode.Parse(await response.Content.ReadAsStringAsync())!;

    private static async Task<string[]> ListAliasesAsync(HttpClient client, string query)
    {
        var response = await client.GetAsync($"{Prompts}{query}");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await ReadJsonAsync(response);
        return body["items"]!.AsArray().Select(i => i!["alias"]!.GetValue<string>()).ToArray();
    }

    #endregion
}
