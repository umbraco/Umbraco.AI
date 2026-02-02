namespace Umbraco.AI.Core.Tools;

/// <summary>
/// Marker interface for system tools that are always included in agent requests.
/// </summary>
/// <remarks>
/// This interface is internal to prevent external packages from marking their tools as system tools.
/// System tools:
/// <list type="bullet">
///     <item>Are always included in agent chat requests (cannot be removed)</item>
///     <item>Are hidden from permission/selection UIs</item>
///     <item>Cannot be overridden by user tools</item>
///     <item>Skip approval workflows</item>
/// </list>
/// </remarks>
internal interface IAISystemTool : IAiTool { }
