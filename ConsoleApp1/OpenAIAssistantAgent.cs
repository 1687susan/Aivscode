using System;
using System.Threading;
using System.Threading.Tasks;

namespace day1
{
    public class OpenAIAssistantAgent : IDisposable
    {
        private readonly OpenAIAssistantWrapper _wrapper;
        private string? _assistantId;
        private string? _currentThreadId;
        private readonly AgentConfig _config;
        private readonly OpenAIAssistantConfig _assistantConfig;
        private bool _isInitialized = false;

        public OpenAIAssistantAgent(string apiKey, AgentConfig config)
        {
            _config = config;
            _assistantConfig = OpenAIAssistantConfigManager.LoadConfig();
            _wrapper = new OpenAIAssistantWrapper(apiKey);
        }

        public async Task InitializeAsync()
        {
            try
            {
                Console.WriteLine(_assistantConfig.OpenAIAssistant.Messages.Initializing);
                
                // Create assistant with MCP tools
                _assistantId = await _wrapper.CreateAssistantAsync(
                    _assistantConfig.OpenAIAssistant.AssistantName, 
                    _assistantConfig.OpenAIAssistant.SystemPrompt);

                Console.WriteLine(OpenAIAssistantConfigManager.FormatMessage(
                    _assistantConfig.OpenAIAssistant.Messages.AssistantCreated, _assistantId));
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(OpenAIAssistantConfigManager.FormatMessage(
                    _assistantConfig.OpenAIAssistant.Messages.InitializationFailed, ex.Message));
                throw;
            }
        }

        public async Task<string> ProcessRequestAsync(string userInput, CancellationToken cancellationToken = default)
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }

            try
            {
                // Create new thread for each conversation if needed
                if (string.IsNullOrEmpty(_currentThreadId))
                {
                    _currentThreadId = await _wrapper.CreateThreadAsync();
                    Console.WriteLine(OpenAIAssistantConfigManager.FormatMessage(
                        _assistantConfig.OpenAIAssistant.Messages.ThreadCreated, _currentThreadId));
                }

                Console.WriteLine(_assistantConfig.OpenAIAssistant.Messages.ProcessingRequest);
                
                // Send message and get response
                var response = await _wrapper.SendMessageAsync(_currentThreadId, userInput, cancellationToken);
                
                return response;
            }
            catch (Exception ex)
            {
                return OpenAIAssistantConfigManager.FormatMessage(
                    _assistantConfig.OpenAIAssistant.Messages.RequestError, ex.Message);
            }
        }

        public Task<string> GetCapabilitiesAsync()
        {
            var capabilitiesText = OpenAIAssistantConfigManager.BuildCapabilitiesText(
                _assistantConfig.OpenAIAssistant.Capabilities);
            return Task.FromResult(capabilitiesText);
        }

        // Public method to reset conversation thread
        public async Task StartNewConversationAsync()
        {
            try
            {
                _currentThreadId = await _wrapper.CreateThreadAsync();
                Console.WriteLine(OpenAIAssistantConfigManager.FormatMessage(
                    _assistantConfig.OpenAIAssistant.Messages.NewConversationCreated, _currentThreadId));
            }
            catch (Exception ex)
            {
                Console.WriteLine(OpenAIAssistantConfigManager.FormatMessage(
                    _assistantConfig.OpenAIAssistant.Messages.NewConversationFailed, ex.Message));
            }
        }

        // Public method to get current thread info
        public string GetCurrentThreadInfo()
        {
            return string.IsNullOrEmpty(_currentThreadId) 
                ? _assistantConfig.OpenAIAssistant.Messages.NoActiveThread
                : OpenAIAssistantConfigManager.FormatMessage(
                    _assistantConfig.OpenAIAssistant.Messages.CurrentThread, _currentThreadId);
        }

        public async Task<bool> ProcessAsync()
        {
            try
            {
                if (!_isInitialized)
                {
                    await InitializeAsync();
                }

                Console.WriteLine(_config.UI.Agents.OpenAIAssistant.ReadyMessage);
                Console.Write(_config.UI.Agents.OpenAIAssistant.InputPrompt);

                string? input;
                while ((input = Console.ReadLine()) is not null)
                {
                    // 檢查退出指令
                    if (input.Equals(_config.UI.Common.ExitCommand, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine(_assistantConfig.OpenAIAssistant.Messages.EndCurrentAgent);
                        return true;
                    }

                    if (input.Equals(_config.UI.Common.BackToMenuCommand, StringComparison.OrdinalIgnoreCase))
                        return true;

                    // 特殊指令：開始新對話
                    if (input.Equals(_assistantConfig.OpenAIAssistant.Commands.New, StringComparison.OrdinalIgnoreCase))
                    {
                        await StartNewConversationAsync();
                        Console.Write(_config.UI.Agents.OpenAIAssistant.InputPrompt);
                        continue;
                    }

                    // 特殊指令：顯示功能
                    if (input.Equals(_assistantConfig.OpenAIAssistant.Commands.Capabilities, StringComparison.OrdinalIgnoreCase))
                    {
                        var capabilities = await GetCapabilitiesAsync();
                        Console.WriteLine(capabilities);
                        Console.Write(_config.UI.Agents.OpenAIAssistant.InputPrompt);
                        continue;
                    }

                    // 處理用戶輸入
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        try
                        {
                            var response = await ProcessRequestAsync(input);
                            Console.WriteLine(OpenAIAssistantConfigManager.FormatMessage(
                                _assistantConfig.OpenAIAssistant.Messages.AssistantResponse, response));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(OpenAIAssistantConfigManager.FormatMessage(
                                _assistantConfig.OpenAIAssistant.Messages.ProcessingFailed, ex.Message));
                        }
                    }

                    Console.Write(_config.UI.Agents.OpenAIAssistant.InputPrompt);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(OpenAIAssistantConfigManager.FormatMessage(
                    _assistantConfig.OpenAIAssistant.Messages.ExecutionError, ex.Message));
                return true;
            }
        }

        public void Dispose()
        {
            // Clean up resources if needed
            _currentThreadId = null;
            _assistantId = null;
        }
    }
}