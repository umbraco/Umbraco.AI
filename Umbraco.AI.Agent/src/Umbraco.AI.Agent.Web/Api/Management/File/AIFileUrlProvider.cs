using Umbraco.AI.Agent.Core.FileStore;

namespace Umbraco.AI.Agent.Web.Api.Management.File;

/// <summary>
/// Provides URLs for the file serving endpoint.
/// </summary>
internal sealed class AIFileUrlProvider : IAIFileUrlProvider
{
    private const string FileEndpointTemplate = "/umbraco/ai/management/api/v1/files/{0}/{1}";

    /// <inheritdoc />
    public string GetFileUrl(string threadId, string fileId)
        => string.Format(FileEndpointTemplate, threadId, fileId);
}
