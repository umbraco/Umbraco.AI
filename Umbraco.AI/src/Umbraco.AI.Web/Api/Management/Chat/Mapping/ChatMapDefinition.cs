using Microsoft.Extensions.AI;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.AI.Web.Api.Management.Chat.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Chat.Mapping;

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

        // Multimodal content — build AIContent list
        if (source.ContentParts is { Count: > 0 })
        {
            var contents = new List<AIContent>();
            foreach (var part in source.ContentParts)
            {
                switch (part)
                {
                    case TextChatContentPartModel text:
                        contents.Add(new TextContent(text.Text));
                        break;
                    case BinaryChatContentPartModel binary:
                        var bytes = Convert.FromBase64String(binary.Data);
                        contents.Add(new DataContent(bytes, binary.MimeType) { Name = binary.Filename });
                        break;
                }
            }
            return new ChatMessage(role, contents);
        }

        // Plain text fallback
#pragma warning disable CS0618 // Type or member is obsolete
        return new ChatMessage(role, source.Content ?? string.Empty);
#pragma warning restore CS0618
    }

    // Umbraco.Code.MapAll
    private static void Map(ChatResponse source, ChatResponseModel target, MapperContext context)
    {
        target.Message = new ChatMessageModel
        {
            Role = "assistant",
#pragma warning disable CS0618 // Type or member is obsolete
            Content = source.Text ?? string.Empty
#pragma warning restore CS0618
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
