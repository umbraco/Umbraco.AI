namespace Umbraco.AI.Core.Settings;

internal class AISettingAttribute(string? key = null) : Attribute
{
    public string? Key { get; } = key;
}