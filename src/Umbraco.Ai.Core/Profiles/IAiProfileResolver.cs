using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Profiles;

/// <summary>
/// Defines a contract for resolving AI profiles based on capability and name.
/// </summary>
public interface IAiProfileResolver
{
    /// <summary>
    /// Gets a specific profile by name and capability.
    /// </summary>
    /// <param name="profileAlias"></param>
    /// <returns></returns>
    AiProfile GetProfile(string profileAlias);
    
    /// <summary>
    /// Gets all profiles for the specified capability.
    /// </summary>
    /// <param name="capability"></param>
    /// <returns></returns>
    IEnumerable<AiProfile> GetProfiles(AiCapability capability);
    
    /// <summary>
    /// Gets the default profile for the specified capability.
    /// </summary>
    /// <param name="capability"></param>
    /// <returns></returns>
    AiProfile GetDefaultProfile(AiCapability capability);
}