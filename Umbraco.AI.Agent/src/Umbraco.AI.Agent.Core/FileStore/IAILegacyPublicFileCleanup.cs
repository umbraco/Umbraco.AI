namespace Umbraco.AI.Agent.Core.FileStore;

/// <summary>
/// Removes conversation uploads left in the publicly served media directory by earlier versions.
/// </summary>
public interface IAILegacyPublicFileCleanup
{
    /// <summary>
    /// Deletes the legacy directory if it is present.
    /// </summary>
    /// <returns><c>true</c> if a directory was found and deleted; otherwise <c>false</c>.</returns>
    /// <remarks>Safe to call repeatedly; does nothing once the directory is gone.</remarks>
    bool DeleteLegacyFiles();
}
