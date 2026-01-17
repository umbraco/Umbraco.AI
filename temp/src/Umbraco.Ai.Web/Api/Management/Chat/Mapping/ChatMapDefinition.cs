using Microsoft.Extensions.AI;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Ai.Web.Api.Management.Chat.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Web.Api.Management.Chat.Mapping;

/// <summary>
/// Map definitions for Chat models.
/// </summary>
public class ChatMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        mapper.Define<ChatMessageModel, ChatMessage>(Map);
        mapper.Define<ChatResponse, ChatResponseModel>((_, _) => new ChatResponseModel(), Map);
        //mapper.Define<ChatResponseUpdate, ChatStreamChunkModel>((_, _) => new ChatStreamChunkModel(), Map);
    }

    // Umbraco.Code.MapAll
    private static ChatMessage Map(ChatMessageModel source, MapperContext context)
    {
        var role = source.Role.ToLowerInvariant() switch
        {
            "system" => ChatRole.System,
            "user" => ChatRole.User,
            "assistant" => ChatRole.Assistant,
            _ => ChatRole.User
        };

        return new ChatMessage(role, source.Content);
    }

    // Umbraco.Code.MapAll
    private static void Map(ChatResponse source, ChatResponseModel target, MapperContext context)
    {
        target.Message = new ChatMessageModel
        {
            Role = "assistant",
            Content = source.Text ?? string.Empty
        };
        target.FinishReason = source.FinishReason?.ToString();
        target.Usage = source.Usage is not null ? context.Map<UsageModel>(source.Usage) : null;
    }

    // // Umbraco.Code.MapAll
    // private static void Map(ChatResponseUpdate source, ChatStreamChunkModel target, MapperContext context)
    // {
    //     target.Content = source.Text;
    //     target.FinishReason = source.FinishReason?.ToString();
    //     target.IsComplete = source.FinishReason is not null;
    // }
}
