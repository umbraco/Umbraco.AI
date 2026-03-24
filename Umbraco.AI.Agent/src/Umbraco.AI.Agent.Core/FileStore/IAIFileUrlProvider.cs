namespace Umbraco.AI.Agent.Core.FileStore;

/// <summary>
/// Provides URL generation for files stored in <see cref="IAIFileStore"/>.
/// </summary>
public interface IAIFileUrlProvider
{
    /// <summary>
    /// Gets the URL where a stored file can be retrieved.
    /// </summary>
    /// <param name="threadId">The conversation thread ID.</param>
    /// <param name="fileId">The file ID.</param>
    /// <returns>A URL that can be used to fetch the file.</returns>
    string GetFileUrl(string threadId, string fileId);
}
