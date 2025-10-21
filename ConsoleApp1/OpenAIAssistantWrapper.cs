using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Chat;

namespace day1
{
    public class OpenAIAssistantWrapper
    {
        private readonly OpenAIClient _client;
        private readonly ChatClient _chatClient;
        
        public OpenAIAssistantWrapper(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentNullException(nameof(apiKey));
            
            _client = new OpenAIClient(apiKey);
            _chatClient = _client.GetChatClient("gpt-4");
        }

        public async Task<string> CreateAssistantAsync(string name = "Multi-Service Assistant", string? instructions = null)
        {
            // For now, just return a simulated assistant ID
            await Task.CompletedTask;
            return "simulated-assistant-id";
        }

        public async Task<string> CreateThreadAsync()
        {
            // For now, just return a simulated thread ID
            await Task.CompletedTask;
            return "simulated-thread-id";
        }

        public async Task<string> SendMessageAsync(string threadId, string message, CancellationToken ct = default)
        {
            try
            {
                // Use basic chat completion with ASCII-safe system message
                var systemMessage = "You are a helpful assistant that can handle customer service, weather queries, HR management, and order processing. Please respond in Traditional Chinese and provide detailed and useful information.";

                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(systemMessage),
                    new UserChatMessage(message)
                };

                var completion = await _chatClient.CompleteChatAsync(messages, cancellationToken: ct);
                return completion.Value.Content.FirstOrDefault()?.Text ?? "No response received";
            }
            catch (Exception ex)
            {
                return $"❌ OpenAI API 錯誤: {ex.Message}";
            }
        }



        // Simple completion fallback (non-Assistant API)
        public async Task<string> GetSimpleCompletionAsync(string prompt)
        {
            var messages = new List<ChatMessage>
            {
                new UserChatMessage(prompt)
            };
            
            var completion = await _chatClient.CompleteChatAsync(messages);
            return completion.Value.Content.FirstOrDefault()?.Text ?? "No response";
        }
    }
}
