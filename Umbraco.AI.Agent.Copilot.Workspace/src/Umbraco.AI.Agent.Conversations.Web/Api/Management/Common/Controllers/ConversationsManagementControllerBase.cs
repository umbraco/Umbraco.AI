using Umbraco.AI.Web.Api.Management.Common.Controllers;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Common.Controllers;

/// <summary>
/// Common base for the Conversations and Projects management controllers. Host-agnostic by design: it
/// carries no OpenAPI-document binding, no named JSON options, and no section policy of its own — it only
/// inherits the backoffice-access policy from <see cref="UmbracoAIManagementControllerBase"/>.
/// </summary>
/// <remarks>
/// The hosting product is responsible for surfacing these controllers in its own OpenAPI document and
/// gating them behind its own section policy (protecting the stored corpus, not just the UI). It does so
/// with an <c>IApplicationModelConvention</c> that targets this type and adds the product's
/// <c>[MapToApi]</c>, named JSON options, and section <c>[Authorize]</c> at runtime. Keeping the binding
/// out of this assembly is what lets the Conversations web layer be reused under a different host.
/// </remarks>
public abstract class ConversationsManagementControllerBase : UmbracoAIManagementControllerBase
{
}
