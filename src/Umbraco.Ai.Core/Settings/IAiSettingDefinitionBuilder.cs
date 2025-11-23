using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Core.Settings;

/// <summary>
/// Defines a contract for building AI setting definitions.
/// </summary>
public interface IAiSettingDefinitionBuilder
{
    /// <summary>
    /// Builds setting definitions for the specified provider and settings type.
    /// </summary>
    /// <param name="providerId"></param>
    /// <typeparam name="TSettings"></typeparam>
    /// <returns></returns>
    IReadOnlyList<AiSettingDefinition> BuildForType<TSettings>(string providerId)
        where TSettings : class;
}