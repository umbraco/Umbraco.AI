using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Common.Controllers;
using Umbraco.AI.Agent.Copilot.Workspace.Core;
using Umbraco.AI.Agent.Copilot.Workspace.Core.Authorization;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Common.Filters;

namespace Umbraco.AI.Agent.Copilot.Workspace.Web.Configuration;

/// <summary>
/// Binds the host-agnostic Conversations/Projects CRUD controllers (from
/// <c>Umbraco.AI.Agent.Conversations.Web</c>) into the Copilot Workspace product at startup: it maps them
/// to this product's single OpenAPI document, applies its named JSON options, and gates them behind the
/// Copilot Workspace section-access policy. Applying the section policy here — on the CRUD controllers,
/// not just the UI — protects the stored corpus (F-SEC).
/// </summary>
/// <remarks>
/// This convention is what lets the Conversations web assembly stay free of any Copilot Workspace
/// dependency: the product-specific binding lives here in the host, not on the controllers. The Umbraco
/// OpenAPI document builder reads <see cref="MapToApiAttribute"/> from endpoint metadata at runtime, so
/// adding it via the application model (rather than a compile-time attribute) is fully supported.
/// </remarks>
internal sealed class CopilotWorkspaceConversationsApiConvention : IApplicationModelConvention
{
    /// <inheritdoc />
    public void Apply(ApplicationModel application)
    {
        foreach (ControllerModel controller in application.Controllers)
        {
            if (typeof(ConversationsManagementControllerBase).IsAssignableFrom(controller.ControllerType) == false)
            {
                continue;
            }

            foreach (SelectorModel selector in controller.Selectors)
            {
                selector.EndpointMetadata.Add(
                    new MapToApiAttribute(CopilotWorkspaceConstants.ManagementApi.ApiName));
                selector.EndpointMetadata.Add(
                    new JsonOptionsNameAttribute(CopilotWorkspaceConstants.ManagementApi.ApiName));

                // Stacks on top of the backoffice-access policy the controllers already carry; the
                // authorization middleware requires every IAuthorizeData in endpoint metadata to pass.
                selector.EndpointMetadata.Add(
                    new AuthorizeAttribute(CopilotWorkspaceAuthorizationPolicies.SectionAccessCopilotWorkspace));
            }
        }
    }
}
