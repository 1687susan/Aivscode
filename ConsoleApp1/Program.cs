// 主要引用的外部檔案與用途說明：
//
// 1. CustomerServicePlugin.cs
//    └ 客戶服務代理人功能（查詢客戶資訊等）
// 2. WeatherServicePlugin.cs
//    └ 天氣服務代理人功能（查詢天氣等）
// 3. HRManagementPlugin.cs
//    └ 人資管理代理人功能（查詢人員、假勤等）
// 4. OrderManagementPlugin.cs
//    └ 訂單管理代理人功能（查詢訂單、庫存等）
// 5. DataStore.cs
//    └ 資料存取層，供各 Plugin 查詢客戶、訂單、天氣、人資等資料
// 6. agent-config.json 或 config.json
//    └ UI 設定、代理人系統提示、主選單選項等（由 ConfigManager.LoadConfig() 載入）
//
// 只要在 PluginManager 註冊新 Plugin，並在 agent-config.json 增加對應 UI 設定，即可擴充新代理人。

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.ComponentModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// 本程式會呼叫以下外部檔案：
// 1. CustomerServicePlugin.cs、OrderManagementPlugin.cs、WeatherServicePlugin.cs、HRManagementPlugin.cs
//    └ 各代理人功能插件，提供具體查詢/管理邏輯
// 2. DataStore.cs
//    └ 資料存取層，供插件查詢客戶、訂單等資料
// 3. 設定 JSON 檔案（如 config.json）
//    └ 可由 ConfigManager.LoadConfig() 載入，決定 UI、代理人、系統提示等參數
//
// 若要擴充代理人或功能，請新增對應 Plugin 類別檔案，並在 PluginManager 註冊。

namespace day1
{
    public class AzureOpenAIConfig
    {
        public string ApiKey { get; set; } = "";
        public string Endpoint { get; set; } = "";
        public string DeploymentName { get; set; } = "";
    }

    public static class AppSettings
    {
        public static AzureOpenAIConfig AzureOpenAI { get; private set; } = new();
        static AppSettings()
        {
            var path = "ConsoleApp1/appsettings.json";
            if (!System.IO.File.Exists(path)) return;
            var json = System.IO.File.ReadAllText(path);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("AzureOpenAI", out var azureOpenAI))
            {
                AzureOpenAI = System.Text.Json.JsonSerializer.Deserialize<AzureOpenAIConfig>(azureOpenAI.GetRawText()) ?? new();
            }
        }
    }

    // 代理人 UI 設定（各代理人顯示訊息）
    public class AgentsUIConfig
    {
        public AgentUIConfig CustomerService { get; set; } = new();
        public AgentUIConfig WeatherService { get; set; } = new();
        public AgentUIConfig HRManagement { get; set; } = new();
        public AgentUIConfig OrderManagement { get; set; } = new();
    }

    // 單一代理人 UI 設定
    public class AgentUIConfig
    {
        public string StartMessage { get; set; } = "";
        public string ReadyMessage { get; set; } = "";
        public string InputPrompt { get; set; } = "";
    }

    // 共用 UI 設定（助理提示、離開指令）
    public class CommonUIConfig
    {
        public string AssistantPrompt { get; set; } = "";
        public string ExitCommand { get; set; } = "";
        public string BackToMenuCommand { get; set; } = "";
    }

    // 代理人類型列舉
    public enum AgentType
    {
        CustomerService,
        WeatherService,
        HRManagement,
        OrderManagement
    }
    // 主選單 UI 設定
    public class MainMenuConfig
    {
        public string Title { get; set; } = "";
        public string SelectService { get; set; } = "";
    [System.Text.Json.Serialization.JsonPropertyName("Options")]
    public Dictionary<string, string> Options { get; set; } = new();
        public string InputPrompt { get; set; } = "";
        public string InvalidOption { get; set; } = "";
        public string InvalidNumber { get; set; } = "";
        public string GoodBye { get; set; } = "";
        public string NumberRange { get; set; } = "";
    }

    // 代理人組態（包含系統提示與 UI 設定）
    public class AgentConfig
    {
        public SystemPromptsConfig SystemPrompts { get; set; } = new();
        public UIConfig UI { get; set; } = new();
    }

    // 各代理人系統提示
        public class SystemPromptsConfig
        {
            public string CustomerService { get; set; } = "";
            public string WeatherService { get; set; } = "";
            public string HRManagement { get; set; } = "";
            public string OrderManagement { get; set; } = "";
        }

    // UI 設定（主選單、各代理人、共用）
        public class UIConfig
        {
            public MainMenuConfig MainMenu { get; set; } = new();
            public AgentsUIConfig Agents { get; set; } = new();
            public CommonUIConfig Common { get; set; } = new();
        }

    // 組態管理（載入設定）
        public static class ConfigManager
        {
            public static AgentConfig LoadConfig()
            {
                var path = "ConsoleApp1/agent-config.json";
                if (!System.IO.File.Exists(path))
                    return new AgentConfig();
                var json = System.IO.File.ReadAllText(path);
                try
                {
                    var config = System.Text.Json.JsonSerializer.Deserialize<AgentConfig>(json, new System.Text.Json.JsonSerializerOptions {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    });
                    return config ?? new AgentConfig();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[反序列化錯誤] {ex.Message}");
                    Console.ResetColor();
                    return new AgentConfig();
                }
            }
        }

    // 代理人管理（執行代理人）
        public static class AgentManager
        {
            public static async Task<bool> RunAgent(AgentType agentType, AgentConfig config)
            {
                BaseAgent agent = agentType switch
                {
                    AgentType.CustomerService => new CustomerServiceAgent(config),
                    AgentType.WeatherService => new WeatherServiceAgent(config),
                    AgentType.HRManagement => new HRManagementAgent(config),
                    AgentType.OrderManagement => new OrderManagementAgent(config),
                    _ => throw new ArgumentException($"未知的 Agent 類型: {agentType}")
                };

                return await agent.ProcessAsync();
            }
        }

    // 代理人基底類別
        public abstract class BaseAgent
        {
            protected readonly AgentConfig _config;
            protected abstract AgentType AgentType { get; }
            protected abstract string SystemPrompt { get; }
            protected abstract AgentUIConfig UIConfig { get; }

            protected BaseAgent(AgentConfig config)
            {
                _config = config;
            }

            protected abstract void ConfigurePlugins(Microsoft.SemanticKernel.Kernel kernel);

            // 顯示對話歷史的方法
            private void ShowChatHistory(Microsoft.SemanticKernel.ChatCompletion.ChatHistory history)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\n=== 📜 對話歷史內容 ===");
                Console.ResetColor();
                
                for (int i = 0; i < history.Count; i++)
                {
                    var message = history[i];
                    string roleColor = message.Role.ToString() switch
                    {
                        "System" => "Magenta",
                        "User" => "Green", 
                        "Assistant" => "Blue",
                        _ => "White"
                    };
                    
                    Console.ForegroundColor = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), roleColor);
                    Console.WriteLine($"[{i + 1}] {message.Role}: {message.Content}");
                    Console.ResetColor();
                    Console.WriteLine(); // 空行分隔
                }
                
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"總共 {history.Count} 則訊息");
                Console.WriteLine("========================\n");
                Console.ResetColor();
            }

            public async Task<bool> ProcessAsync()
            {
                var kernel = Microsoft.SemanticKernel.Kernel.CreateBuilder()
                    .AddAzureOpenAIChatCompletion(
                        deploymentName: AppSettings.AzureOpenAI.DeploymentName,
                        endpoint: AppSettings.AzureOpenAI.Endpoint,
                        apiKey: AppSettings.AzureOpenAI.ApiKey
                    )
                    .Build();

                ConfigurePlugins(kernel);

                var settings = new Microsoft.SemanticKernel.Connectors.OpenAI.OpenAIPromptExecutionSettings
                {
                    // FunctionChoiceBehavior = Microsoft.SemanticKernel.Connectors.OpenAI.FunctionChoiceBehavior.Auto()
                };

                var history = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
                history.AddDeveloperMessage(SystemPrompt);

                // Debug: 顯示初始化的對話歷史
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("[Debug] 初始化對話歷史:");
                Console.WriteLine($"系統提示: {SystemPrompt}");
                Console.ResetColor();

                var chatService = kernel.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>();

                Console.WriteLine(UIConfig.ReadyMessage);
                Console.Write(UIConfig.InputPrompt);

                return await ProcessConversationLoop(chatService, history, settings, kernel);
            }

            private async Task<bool> ProcessConversationLoop(
                Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService chatService,
                Microsoft.SemanticKernel.ChatCompletion.ChatHistory history,
                Microsoft.SemanticKernel.Connectors.OpenAI.OpenAIPromptExecutionSettings settings,
                Microsoft.SemanticKernel.Kernel kernel)
            {
                string? input;
                while ((input = Console.ReadLine()) is not null)
                {
                    if (input.Equals(_config.UI.Common.ExitCommand, StringComparison.OrdinalIgnoreCase))
                        return false; // 退出程式
                    
                    if (input.Equals(_config.UI.Common.BackToMenuCommand, StringComparison.OrdinalIgnoreCase))
                        return true; // 回到主選單

                    // 特殊指令：查看對話歷史
                    if (input.Equals("history", StringComparison.OrdinalIgnoreCase))
                    {
                        ShowChatHistory(history);
                        Console.Write(UIConfig.InputPrompt);
                        continue;
                    }

                    history.AddUserMessage(input);

                    var result = chatService.GetStreamingChatMessageContentsAsync(history, settings, kernel: kernel);

                    string response = "";
                    bool first = true;
                    await foreach (var content in result)
                    {
                        if (content.Role.HasValue && first)
                        {
                            Console.Write(_config.UI.Common.AssistantPrompt);
                            first = false;
                        }
                        Console.Write(content.Content);
                        response += content.Content;
                    }
                    Console.WriteLine();

                    history.AddAssistantMessage(response);
                    Console.Write(UIConfig.InputPrompt);
                }
                return false; // 正常退出（沒有輸入）
            }
        }

    // 主程式入口
        class Program
        {
            static async Task Main(string[] args)
            {
                if (args.Length > 0 && args[0] == "--test")
                {
                    await TestProgram.TestMain(args);
                    return;
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[Debug] 進入 Main 方法，開始載入 config...");
                Console.ResetColor();
                var config = ConfigManager.LoadConfig();

                if (config?.UI?.MainMenu == null || config.UI.MainMenu.Options == null || config.UI.MainMenu.Options.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[錯誤] 主選單設定載入失敗，請確認 agent-config.json 是否存在且格式正確。");
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[Debug] config.UI.MainMenu: {System.Text.Json.JsonSerializer.Serialize(config?.UI?.MainMenu)}");
                    Console.WriteLine($"[Debug] Options: {System.Text.Json.JsonSerializer.Serialize(config?.UI?.MainMenu?.Options)}");
                    Console.ResetColor();
                    return;
                }

                // 主選單迴圈
                while (true)
                {
                    Console.WriteLine("\n" + config.UI.MainMenu.Title);
                    Console.WriteLine(config.UI.MainMenu.SelectService);
                    foreach (var option in config.UI.MainMenu.Options)
                    {
                        Console.WriteLine($"{option.Key}. {option.Value}");
                    }
                    Console.Write(config.UI.MainMenu.InputPrompt);

                    var input = Console.ReadLine();
                    var choice = input?.Trim().Replace("\r", "").Replace("\n", "").Replace(" ", "").Replace("\u0000", "").Replace("\uFEFF", "");
                    if (string.IsNullOrWhiteSpace(choice))
                    {
                        choice = "";
                    }

                    bool backToMenu = false;
                    // 引用 agent-config.json
                    switch (choice)
                    {
                        case "1":
                            Console.WriteLine(config.UI.Agents.CustomerService.StartMessage);
                            backToMenu = await AgentManager.RunAgent(AgentType.CustomerService, config);
                            break;
                        case "2":
                            Console.WriteLine(config.UI.Agents.WeatherService.StartMessage);
                            backToMenu = await AgentManager.RunAgent(AgentType.WeatherService, config);
                            break;
                        case "3":
                            Console.WriteLine(config.UI.Agents.HRManagement.StartMessage);
                            backToMenu = await AgentManager.RunAgent(AgentType.HRManagement, config);
                            break;
                        case "4":
                            Console.WriteLine(config.UI.Agents.OrderManagement.StartMessage);
                            backToMenu = await AgentManager.RunAgent(AgentType.OrderManagement, config);
                            break;
                        case "5":
                            Console.WriteLine($"\n{config.UI.MainMenu.GoodBye}");
                            return;
                        case "":
                            Console.WriteLine($"\n{config.UI.MainMenu.InvalidNumber}");
                            continue;
                        default:
                            Console.WriteLine($"\n{string.Format(config.UI.MainMenu.InvalidOption, choice)}");
                            Console.WriteLine(config.UI.MainMenu.NumberRange);
                            continue;
                    }

                    // 如果代理人返回 false (exit)，則退出程式
                    if (!backToMenu)
                    {
                        Console.WriteLine($"\n{config.UI.MainMenu.GoodBye}");
                        return;
                    }
                    // 如果返回 true (menu)，則繼續迴圈回到主選單
                }
            }
        }

        public static class PluginManager
        {
            public static void ConfigureForCustomerService(Microsoft.SemanticKernel.Kernel kernel)
            {
                kernel.Plugins.AddFromObject(new CustomerServicePlugin(), "CustomerService");
                kernel.Plugins.AddFromObject(new OrderManagementPlugin(), "OrderManagement");
            }

            public static void ConfigureForWeatherService(Microsoft.SemanticKernel.Kernel kernel)
            {
                kernel.Plugins.AddFromObject(new WeatherServicePlugin(), "WeatherService");
            }

            public static void ConfigureForHRService(Microsoft.SemanticKernel.Kernel kernel)
            {
                kernel.Plugins.AddFromObject(new HRManagementPlugin(), "HRManagement");
            }

            public static void ConfigureForOrderManagement(Microsoft.SemanticKernel.Kernel kernel)
            {
                kernel.Plugins.AddFromObject(new OrderManagementPlugin(), "OrderManagement");
            }
        }


        public class CustomerServiceAgent : BaseAgent
        {
            public CustomerServiceAgent(AgentConfig config) : base(config) { }

            protected override AgentType AgentType => AgentType.CustomerService;
            protected override string SystemPrompt => _config.SystemPrompts.CustomerService;
            protected override AgentUIConfig UIConfig => _config.UI.Agents.CustomerService;

            protected override void ConfigurePlugins(Microsoft.SemanticKernel.Kernel kernel)
            {
                PluginManager.ConfigureForCustomerService(kernel);
            }
        }

        public class WeatherServiceAgent : BaseAgent
        {
            public WeatherServiceAgent(AgentConfig config) : base(config) { }

            protected override AgentType AgentType => AgentType.WeatherService;
            protected override string SystemPrompt => _config.SystemPrompts.WeatherService;
            protected override AgentUIConfig UIConfig => _config.UI.Agents.WeatherService;

            protected override void ConfigurePlugins(Microsoft.SemanticKernel.Kernel kernel)
            {
                PluginManager.ConfigureForWeatherService(kernel);
            }
        }

        public class HRManagementAgent : BaseAgent
        {
            public HRManagementAgent(AgentConfig config) : base(config) { }

            protected override AgentType AgentType => AgentType.HRManagement;
            protected override string SystemPrompt => _config.SystemPrompts.HRManagement;
            protected override AgentUIConfig UIConfig => _config.UI.Agents.HRManagement;

            protected override void ConfigurePlugins(Microsoft.SemanticKernel.Kernel kernel)
            {
                PluginManager.ConfigureForHRService(kernel);
            }
        }

        public class OrderManagementAgent : BaseAgent
        {
            public OrderManagementAgent(AgentConfig config) : base(config) { }

            protected override AgentType AgentType => AgentType.OrderManagement;
            protected override string SystemPrompt => _config.SystemPrompts.OrderManagement;
            protected override AgentUIConfig UIConfig => _config.UI.Agents.OrderManagement;

            protected override void ConfigurePlugins(Microsoft.SemanticKernel.Kernel kernel)
            {
                PluginManager.ConfigureForOrderManagement(kernel);
            }
        }
    }
