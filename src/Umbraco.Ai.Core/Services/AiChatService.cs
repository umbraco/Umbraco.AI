// using System.Runtime.CompilerServices;
// using Microsoft.Extensions.AI;
// using Microsoft.Extensions.Options;
// using Umbraco.Ai.Core.Models;
// using Umbraco.Ai.Core.Profiles;
// using Umbraco.Ai.Core.Registry;
//
// namespace Umbraco.Ai.Core.Services;
//
// internal sealed class AiChatService : IAiChatService
// {
//     private readonly IAiRegistry _registry;
//     private readonly IAiProfileResolver _profileResolver;
//     private readonly AiOptions _options;
//
//     public AiChatService(
//         IAiRegistry registry,
//         IAiProfileResolver profileResolver,
//         IOptionsMonitor<AiOptions> options)
//     {
//         _registry = registry;
//         _profileResolver = profileResolver;
//         _options = options.CurrentValue;
//     }
//
//     public async Task<string> GenerateTextAsync(string prompt, CancellationToken cancellationToken = default)
//     {
//         var profile = _profileResolver.GetDefaultProfile(AiCapability.Chat);
//         return await GenerateTextFromProfileAsync(profile.Name, prompt, cancellationToken);
//     }
//
//     public async Task<string> GenerateTextFromProfileAsync(string profileName, string prompt, CancellationToken cancellationToken = default)
//     {
//         var profile = _profileResolver.GetProfile(profileName, AiCapability.Chat);
//
//         var request = new AiChatRequest
//         {
//             ProfileName = profileName,
//             Model = profile.Model,
//             Temperature = profile.Temperature,
//             MaxTokens = profile.MaxTokens,
//             Messages = new List<AiChatMessage>
//             {
//                 new() { Role = AiChatRole.User, Text = prompt }
//             }
//         };
//
//         // Add system message if profile has system prompt
//         if (!string.IsNullOrWhiteSpace(profile.SystemPromptTemplate))
//         {
//             request.Messages.Insert(0, new AiChatMessage
//             {
//                 Role = AiChatRole.System,
//                 Text = profile.SystemPromptTemplate
//             });
//         }
//
//         var response = await GenerateChatAsync(request, cancellationToken);
//         return response.Message.Text;
//     }
//
//     public Task<AiChatResponse> GenerateChatAsync(AiChatRequest request, CancellationToken cancellationToken = default)
//     {
//         // If no model specified, use default profile
//         if (request.Model == null && string.IsNullOrWhiteSpace(request.ProfileName))
//         {
//             var profile = _profileResolver.GetDefaultProfile(AiCapability.Chat);
//             request = ApplyProfile(request, profile);
//         }
//
//         return GenerateChatInternalAsync(request, cancellationToken);
//     }
//
//     public Task<AiChatResponse> GenerateChatFromProfileAsync(string profileName, AiChatRequest request, CancellationToken cancellationToken = default)
//     {
//         var profile = _profileResolver.GetProfile(profileName, AiCapability.Chat);
//         request = ApplyProfile(request, profile);
//         return GenerateChatInternalAsync(request, cancellationToken);
//     }
//
//     public async IAsyncEnumerable<AiChatDeltaResponse> GenerateChatStreamAsync(
//         AiChatRequest request,
//         [EnumeratorCancellation] CancellationToken cancellationToken = default)
//     {
//         // Resolve model reference
//         var modelRef = ResolveModelRef(request);
//
//         // Get chat client from registry
//         var chatClient = _registry.CreateChatClient(modelRef.ProviderAlias, GetConnectionSettings(request));
//
//         // Convert to Microsoft.Extensions.AI messages
//         var messages = ConvertToMeaiMessages(request.Messages);
//
//         // Create chat options
//         var chatOptions = new ChatOptions
//         {
//             Temperature = (float?)request.Temperature,
//             MaxOutputTokens = request.MaxTokens,
//             ModelId = modelRef.ModelId
//         };
//
//         // Stream from MEAI client
//         await foreach (var update in chatClient.CompleteStreamingAsync(messages, chatOptions, cancellationToken))
//         {
//             var deltaResponse = new AiChatDeltaResponse
//             {
//                 Delta = update.Text ?? string.Empty,
//                 IsFinal = update.FinishReason != null,
//                 Model = modelRef,
//                 ProfileName = request.ProfileName,
//                 Usage = update.Usage != null ? new AiUsage
//                 {
//                     InputTokens = update.Usage.InputTokenCount ?? 0,
//                     OutputTokens = update.Usage.OutputTokenCount ?? 0,
//                     TotalTokens = update.Usage.TotalTokenCount ?? 0
//                 } : null,
//                 RawResponse = update
//             };
//
//             yield return deltaResponse;
//         }
//     }
//
//     private async Task<AiChatResponse> GenerateChatInternalAsync(AiChatRequest request, CancellationToken cancellationToken)
//     {
//         // Resolve model reference
//         var modelRef = ResolveModelRef(request);
//
//         // Get chat client from registry
//         var chatClient = _registry.CreateChatClient(modelRef.ProviderAlias, GetConnectionSettings(request));
//
//         // Convert to Microsoft.Extensions.AI messages
//         var messages = ConvertToMeaiMessages(request.Messages);
//
//         // Create chat options
//         var chatOptions = new ChatOptions
//         {
//             Temperature = (float?)request.Temperature,
//             MaxOutputTokens = request.MaxTokens,
//             ModelId = modelRef.ModelId
//         };
//
//         // Call MEAI client
//         var completion = await chatClient.CompleteAsync(messages, chatOptions, cancellationToken);
//
//         // Convert response
//         var response = new AiChatResponse
//         {
//             Message = new AiChatMessage
//             {
//                 Role = ConvertFromMeaiRole(completion.Message.Role),
//                 Text = completion.Message.Text ?? string.Empty
//             },
//             Model = modelRef,
//             ProfileName = request.ProfileName ?? _options.DefaultChatProfileAlias,
//             Usage = completion.Usage != null ? new AiUsage
//             {
//                 InputTokens = completion.Usage.InputTokenCount ?? 0,
//                 OutputTokens = completion.Usage.OutputTokenCount ?? 0,
//                 TotalTokens = completion.Usage.TotalTokenCount ?? 0
//             } : null,
//             FinishReason = completion.FinishReason?.ToString(),
//             RawResponse = completion
//         };
//
//         return response;
//     }
//
//     private AiModelRef ResolveModelRef(AiChatRequest request)
//     {
//         if (request.Model != null)
//         {
//             return request.Model.Value;
//         }
//
//         if (!string.IsNullOrWhiteSpace(request.ProfileName))
//         {
//             var profile = _profileResolver.GetProfile(request.ProfileName, AiCapability.Chat);
//             return profile.Model;
//         }
//
//         var defaultProfile = _profileResolver.GetDefaultProfile(AiCapability.Chat);
//         return defaultProfile.Model;
//     }
//
//     private static object? GetConnectionSettings(AiChatRequest request)
//     {
//         // Connection settings will come from the connection system (future)
//         // For now, return null to use provider defaults
//         return null;
//     }
//
//     private static List<ChatMessage> ConvertToMeaiMessages(IEnumerable<AiChatMessage> messages)
//     {
//         return messages.Select(m => new ChatMessage(
//             ConvertToMeaiRole(m.Role),
//             m.Text
//         )).ToList();
//     }
//
//     private static ChatRole ConvertToMeaiRole(AiChatRole role)
//     {
//         return role switch
//         {
//             AiChatRole.System => ChatRole.System,
//             AiChatRole.User => ChatRole.User,
//             AiChatRole.Assistant => ChatRole.Assistant,
//             _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown chat role")
//         };
//     }
//
//     private static AiChatRole ConvertFromMeaiRole(ChatRole role)
//     {
//         if (role == ChatRole.System) return AiChatRole.System;
//         if (role == ChatRole.User) return AiChatRole.User;
//         if (role == ChatRole.Assistant) return AiChatRole.Assistant;
//
//         // Default to assistant for unknown roles
//         return AiChatRole.Assistant;
//     }
//
//     private static AiChatRequest ApplyProfile(AiChatRequest request, AiProfile profile)
//     {
//         return new AiChatRequest
//         {
//             Model = profile.Model,
//             Messages = request.Messages,
//             Temperature = request.Temperature ?? profile.Temperature,
//             MaxTokens = request.MaxTokens ?? profile.MaxTokens,
//             Metadata = request.Metadata,
//             ProfileName = profile.Name
//         };
//     }
// }
