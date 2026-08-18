using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.IO;

namespace Umbraco.AI.Agent.Core.FileStore;

/// <summary>
/// Deletes conversation uploads left behind in the media file system by earlier versions.
/// </summary>
/// <remarks>
/// <para>
/// Conversation uploads used to be written to <c>MediaFileManager.FileSystem</c>, which is rooted
/// inside the web root and served at <c>/media</c>. Moving new uploads to a private location does not
/// help with what is already there: those files stay exactly where they are, and because the retention
/// job only ever looks at the store's own file system, they would never be cleaned up either. Left
/// alone they would go from expiring after a day to being publicly readable indefinitely, so the
/// upgrade has to remove them.
/// </para>
/// <para>
/// Deliberately deletes rather than migrates. The files are short-lived by design, the directory is
/// only reachable because of the original defect, and copying them somewhere private would keep
/// content alive past the retention window it was uploaded under.
/// </para>
/// </remarks>
internal sealed class AILegacyPublicFileCleanup : IAILegacyPublicFileCleanup
{
    private readonly MediaFileManager _mediaFileManager;
    private readonly ILogger<AILegacyPublicFileCleanup> _logger;

    public AILegacyPublicFileCleanup(MediaFileManager mediaFileManager, ILogger<AILegacyPublicFileCleanup> logger)
    {
        _mediaFileManager = mediaFileManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool DeleteLegacyFiles()
    {
        const string legacyPath = Constants.SystemDirectories.LegacyPublicConversationFiles;

        try
        {
            if (!_mediaFileManager.FileSystem.DirectoryExists(legacyPath))
            {
                return false;
            }

            _mediaFileManager.FileSystem.DeleteDirectory(legacyPath, recursive: true);

            _logger.LogWarning(
                "Deleted conversation uploads left in the publicly served media directory at '{LegacyPath}'. "
                    + "Earlier versions stored them there, where they could be downloaded without authentication. "
                    + "New uploads are stored outside the web root.",
                legacyPath);

            return true;
        }
        catch (Exception ex)
        {
            // Never let this stop the job or startup: log loudly so it can be removed by hand.
            _logger.LogError(
                ex,
                "Could not delete conversation uploads from the publicly served media directory at '{LegacyPath}'. "
                    + "Delete this directory manually — files in it are readable without authentication.",
                legacyPath);

            return false;
        }
    }
}
