// using Umbraco.Ai.Core.Models;
//
// namespace Umbraco.Ai.Core.Services;
//
// /// <summary>
// /// Defines an AI chat and text generation service.
// /// </summary>
// public interface IAiChatService
// {
//     /// <summary>
//     /// Generates a chat completion based on the provided chat request.
//     /// </summary>
//     /// <param name="request">The chat request containing messages and configuration.</param>
//     /// <param name="cancellationToken">Cancellation token for the async operation.</param>
//     /// <returns>Chat response with generated message.</returns>
//     Task<AiChatResponse> GenerateChatAsync(AiChatRequest request, CancellationToken cancellationToken = default);
//
//     /// <summary>
//     /// Generates a chat completion based on the provided chat request using a specified profile.
//     /// </summary>
//     /// <param name="profileName">The name of the profile to use.</param>
//     /// <param name="request">The chat request containing messages and configuration.</param>
//     /// <param name="cancellationToken">Cancellation token for the async operation.</param>
//     /// <returns>Chat response with generated message.</returns>
//     Task<AiChatResponse> GenerateChatFromProfileAsync(string profileName, AiChatRequest request, CancellationToken cancellationToken = default);
//
//     /// <summary>
//     /// Generates a chat completion in a streaming fashion, yielding message deltas as they are produced.
//     /// </summary>
//     /// <param name="request">The chat request containing messages and configuration.</param>
//     /// <param name="cancellationToken">Cancellation token for the async operation.</param>
//     /// <returns>Async stream of chat delta responses.</returns>
//     IAsyncEnumerable<AiChatDeltaResponse> GenerateChatStreamAsync(AiChatRequest request, CancellationToken cancellationToken = default);
// }
