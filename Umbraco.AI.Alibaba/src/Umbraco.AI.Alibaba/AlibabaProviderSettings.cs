using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.Alibaba;

/// <summary>
/// Settings for the Alibaba Cloud (Qwen) provider.
/// </summary>
public class AlibabaProviderSettings
{
    /// <summary>
    /// The API key for authenticating with Alibaba Cloud Model Studio (DashScope).
    /// </summary>
    [AIField(IsSensitive = true)]
    [Required]
    public string? ApiKey { get; set; }

    /// <summary>
    /// Custom API endpoint URL. Defaults to the international Model Studio endpoint —
    /// override for the China (Beijing) region or a workspace-scoped endpoint.
    /// </summary>
    [AIField]
    public string? Endpoint { get; set; } = "https://dashscope-intl.aliyuncs.com/compatible-mode/v1";
}
