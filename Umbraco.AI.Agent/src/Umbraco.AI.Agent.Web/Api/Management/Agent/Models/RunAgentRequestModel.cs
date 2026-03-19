using System.ComponentModel.DataAnnotations;
using Umbraco.AI.Web.Api.Management.Chat.Models;

namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Models;

/// <summary>
/// Request model for running or streaming an agent.
/// </summary>
public class RunAgentRequestModel
{
    /// <summary>
    /// The chat messages to send to the agent.
    /// </summary>
    [Required]
    [MinLength(1)]
    public IReadOnlyList<ChatMessageModel> Messages { get; set; } = [];
}
