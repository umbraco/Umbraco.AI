namespace Umbraco.Ai.Core.Settings;

internal class AiSettingAttribute(string? key = null) : Attribute
{
    public string? Key { get; } = key;
}