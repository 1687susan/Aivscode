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
using System.Linq;
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

    public class OpenAIConfig
    {
        public string ApiKey { get; set; } = "";
        public string Model { get; set; } = "gpt-4";
        public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
    }

    public class FunctionChoiceConfig
    {
        public string DefaultStrategy { get; set; } = "Smart";
        public bool EnableIntelligentStrategy { get; set; } = true;
        public bool EnableDebugInfo { get; set; } = true;
        public Dictionary<string, string> AgentStrategies { get; set; } = new();
        public ScenarioStrategiesConfig ScenarioStrategies { get; set; } = new();
    }

    public class ScenarioStrategiesConfig
    {
        public List<string> QueryKeywords { get; set; } = new();
        public List<string> ExplanationKeywords { get; set; } = new();
        public bool ForceRequired { get; set; } = true;
        public bool ForceNone { get; set; } = true;
    }

    public static class AppSettings
    {
        public static string AIProvider { get; private set; } = "AzureOpenAI";
        public static AzureOpenAIConfig AzureOpenAI { get; private set; } = new();
        public static OpenAIConfig OpenAI { get; private set; } = new();
        public static FunctionChoiceConfig FunctionChoice { get; private set; } = new();
        static AppSettings()
        {
            var path = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            // 如果在程式目錄找不到，嘗試在當前目錄查找
            if (!System.IO.File.Exists(path))
            {
                path = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "appsettings.json");
                if (!System.IO.File.Exists(path))
                {
                    // 嘗試在專案目錄查找
                    var projectDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    for (int i = 0; i < 5 && projectDir != null; i++) // 最多往上查找5層
                    {
                        var testPath = System.IO.Path.Combine(projectDir, "appsettings.json");
                        if (System.IO.File.Exists(testPath))
                        {
                            path = testPath;
                            break;
                        }
                        projectDir = System.IO.Directory.GetParent(projectDir)?.FullName;
                    }
                }
            }
            
            Console.WriteLine($"[Debug] 載入 appsettings.json: {path}");
            Console.WriteLine($"[Debug] appsettings.json 存在: {System.IO.File.Exists(path)}");
            
            if (!System.IO.File.Exists(path)) return;
            var json = System.IO.File.ReadAllText(path);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            
            // 讀取 AI 提供者設定
            if (doc.RootElement.TryGetProperty("AIProvider", out var providerElement))
            {
                AIProvider = providerElement.GetString() ?? "AzureOpenAI";
            }
            
            // 讀取 Azure OpenAI 設定
            if (doc.RootElement.TryGetProperty("AzureOpenAI", out var azureOpenAI))
            {
                AzureOpenAI = System.Text.Json.JsonSerializer.Deserialize<AzureOpenAIConfig>(azureOpenAI.GetRawText()) ?? new();
                Console.WriteLine($"[Debug] AzureOpenAI 端點: {AzureOpenAI.Endpoint}");
            }
            
            // 讀取 OpenAI 設定
            if (doc.RootElement.TryGetProperty("OpenAI", out var openAI))
            {
                OpenAI = System.Text.Json.JsonSerializer.Deserialize<OpenAIConfig>(openAI.GetRawText()) ?? new();
                Console.WriteLine($"[Debug] OpenAI 模型: {OpenAI.Model}");
            }
            
            // 讀取 Function Choice 設定
            if (doc.RootElement.TryGetProperty("FunctionChoice", out var functionChoice))
            {
                FunctionChoice = System.Text.Json.JsonSerializer.Deserialize<FunctionChoiceConfig>(functionChoice.GetRawText()) ?? new();
                Console.WriteLine($"[Debug] Function Choice 預設策略: {FunctionChoice.DefaultStrategy}");
                Console.WriteLine($"[Debug] 智能策略啟用: {FunctionChoice.EnableIntelligentStrategy}");
            }
            
            Console.WriteLine($"[Debug] 當前使用的 AI 提供者: {AIProvider}");
        }
    }

    // Kernel 工廠，根據設定創建適當的 Kernel
    public static class KernelFactory
    {
        public static Microsoft.SemanticKernel.Kernel CreateKernel()
        {
            var builder = Microsoft.SemanticKernel.Kernel.CreateBuilder();
            
            if (AppSettings.AIProvider == "OpenAI")
            {
                Console.WriteLine("[Info] 使用 OpenAI API");
                builder.AddOpenAIChatCompletion(
                    modelId: AppSettings.OpenAI.Model,
                    apiKey: AppSettings.OpenAI.ApiKey
                );
            }
            else
            {
                Console.WriteLine("[Info] 使用 Azure OpenAI");
                builder.AddAzureOpenAIChatCompletion(
                    deploymentName: AppSettings.AzureOpenAI.DeploymentName,
                    endpoint: AppSettings.AzureOpenAI.Endpoint,
                    apiKey: AppSettings.AzureOpenAI.ApiKey
                );
            }
            
            return builder.Build();
        }
    }

    // 代理人 UI 設定（各代理人顯示訊息）
    public class AgentsUIConfig
    {
        public AgentUIConfig CustomerService { get; set; } = new();
        public AgentUIConfig WeatherService { get; set; } = new();
        public AgentUIConfig HRManagement { get; set; } = new();
        public AgentUIConfig OrderManagement { get; set; } = new();
        public AgentUIConfig OpenAIAssistant { get; set; } = new();
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
        OrderManagement,
        OpenAIAssistant
    }

    // AI 執行計劃
    public class AIExecutionPlan
    {
        public List<AgentExecutionStep> Steps { get; set; } = new();
        public string Reasoning { get; set; } = "";
        public string ExecutionMode { get; set; } = "sequential"; // sequential, parallel, adaptive
        public bool RequiresUserConfirmation { get; set; } = true;
    }

    // 代理人執行步驟
    public class AgentExecutionStep
    {
        public AgentType AgentType { get; set; }
        public string Reason { get; set; } = "";
        public int Priority { get; set; } = 1;
        public List<string> ExpectedInputs { get; set; } = new();
        public string Context { get; set; } = "";
    }

    // AI 調度結果
    public class AISchedulingResult
    {
        public AIExecutionPlan Plan { get; set; } = new();
        public string Analysis { get; set; } = "";
        public List<string> Recommendations { get; set; } = new();
        public bool Success { get; set; } = false;
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
        public string NumberRange { get; set; } = "";
        public string ExecutionOrderPrompt { get; set; } = "";
        public string ExecutionOrderInput { get; set; } = "";
        public string InvalidExecutionOrder { get; set; } = "";
        public string GoodBye { get; set; } = "";
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
                var path = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "agent-config.json");
                // 如果在程式目錄找不到，嘗試在上層目錄查找
                if (!System.IO.File.Exists(path))
                {
                    path = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "agent-config.json");
                    if (!System.IO.File.Exists(path))
                    {
                        // 嘗試在專案目錄查找
                        var projectDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                        for (int i = 0; i < 5 && projectDir != null; i++) // 最多往上查找5層
                        {
                            var testPath = System.IO.Path.Combine(projectDir, "agent-config.json");
                            if (System.IO.File.Exists(testPath))
                            {
                                path = testPath;
                                break;
                            }
                            projectDir = System.IO.Directory.GetParent(projectDir)?.FullName;
                        }
                    }
                }
                
                Console.WriteLine($"[Debug] 嘗試載入設定檔: {path}");
                Console.WriteLine($"[Debug] 設定檔存在: {System.IO.File.Exists(path)}");
                
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

    // AI 智能調度器
    public static class AIScheduler
    {
        public static async Task<AISchedulingResult> AnalyzeAndSchedule(string userRequest, AgentConfig config)
        {
            var kernel = KernelFactory.CreateKernel();

            var systemPrompt = $@"
你是一個專業的 AI 代理人調度器。根據用戶的需求，分析並決定最佳的代理人執行策略。

可用的代理人：
1. 客戶服務專員 (CustomerService) - 處理客戶查詢、客戶資訊管理
2. 天氣預報專員 (WeatherService) - 提供天氣查詢、預報服務
3. 人力資源專員 (HRManagement) - 處理員工資訊、請假、薪資等
4. 訂單管理專員 (OrderManagement) - 處理訂單查詢、庫存管理

請分析用戶需求，並提供 JSON 格式的執行計劃：
{{
  ""steps"": [
    {{
      ""agentType"": ""CustomerService"",
      ""reason"": ""需要查詢客戶資訊"",
      ""priority"": 1,
      ""expectedInputs"": [""客戶ID"", ""客戶姓名""],
      ""context"": ""相關背景資訊""
    }}
  ],
  ""reasoning"": ""選擇這些代理人的原因"",
  ""executionMode"": ""sequential"",
  ""requiresUserConfirmation"": true
}}

請只回傳 JSON，不要額外說明。";

            var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
            chatHistory.AddSystemMessage(systemPrompt);
            chatHistory.AddUserMessage($"用戶需求：{userRequest}");

            var chatService = kernel.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>();
            var response = await chatService.GetChatMessageContentAsync(chatHistory);

            try
            {
                var jsonResponse = response.Content?.Trim();
                if (jsonResponse?.StartsWith("```json") == true)
                {
                    jsonResponse = jsonResponse.Substring(7);
                }
                if (jsonResponse?.EndsWith("```") == true)
                {
                    jsonResponse = jsonResponse.Substring(0, jsonResponse.Length - 3);
                }

                var planData = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(jsonResponse ?? "{}");
                
                var plan = new AIExecutionPlan
                {
                    Reasoning = planData.TryGetProperty("reasoning", out var reasoningProp) ? reasoningProp.GetString() ?? "AI 分析結果" : "AI 分析結果",
                    ExecutionMode = planData.TryGetProperty("executionMode", out var modeProp) ? modeProp.GetString() ?? "sequential" : "sequential",
                    RequiresUserConfirmation = planData.TryGetProperty("requiresUserConfirmation", out var confirmProp) ? confirmProp.GetBoolean() : true
                };

                if (planData.TryGetProperty("steps", out var stepsProperty))
                {
                    foreach (var stepElement in stepsProperty.EnumerateArray())
                    {
                        var agentTypeStr = stepElement.GetProperty("agentType").GetString();
                        if (Enum.TryParse<AgentType>(agentTypeStr, out var agentType))
                        {
                            var step = new AgentExecutionStep
                            {
                                AgentType = agentType,
                                Reason = stepElement.GetProperty("reason").GetString() ?? "",
                                Priority = stepElement.TryGetProperty("priority", out var priorityProp) ? priorityProp.GetInt32() : 1,
                                Context = stepElement.TryGetProperty("context", out var contextProp) ? contextProp.GetString() ?? "" : ""
                            };

                            if (stepElement.TryGetProperty("expectedInputs", out var inputsProp))
                            {
                                foreach (var input in inputsProp.EnumerateArray())
                                {
                                    step.ExpectedInputs.Add(input.GetString() ?? "");
                                }
                            }

                            plan.Steps.Add(step);
                        }
                    }
                }

                return new AISchedulingResult
                {
                    Plan = plan,
                    Analysis = $"AI 分析：{plan.Reasoning}",
                    Recommendations = new List<string> { "建議按照 AI 推薦的順序執行", "可隨時調整執行計劃" },
                    Success = true
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AI 調度錯誤] {ex.Message}");
                return new AISchedulingResult
                {
                    Success = false,
                    Analysis = "AI 分析失敗，將使用預設執行計劃"
                };
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
                    AgentType.OpenAIAssistant => throw new ArgumentException("OpenAI Assistant should use RunOpenAIAssistantAgent method"),
                    _ => throw new ArgumentException($"未知的 Agent 類型: {agentType}")
                };

                return await agent.ProcessAsync();
            }

            public static async Task<bool> RunOpenAIAssistantAgent(AgentConfig config)
            {
                try
                {
                    // Get OpenAI API key from config
                    var apiKey = AppSettings.OpenAI.ApiKey;
                    if (string.IsNullOrEmpty(apiKey) || apiKey.Contains("把您的真實API Key貼在這裡"))
                    {
                        Console.WriteLine("❌ OpenAI API 金鑰未設定或為示例值");
                        Console.WriteLine("📝 請在 appsettings.json 中設定您的 OpenAI API 金鑰");
                        Console.WriteLine("💡 範例格式: \"ApiKey\": \"sk-proj-您的真實API金鑰\"");
                        Console.WriteLine("\n🔄 目前改用模擬回應模式...");
                        
                        // Use simulation mode
                        await RunSimulatedAssistantAsync(config);
                        return true;
                    }

                    using var assistantAgent = new OpenAIAssistantAgent(apiKey, config);
                    return await assistantAgent.ProcessAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ OpenAI Assistant Agent 執行錯誤: {ex.Message}");
                    Console.WriteLine("🔄 切換到模擬模式...");
                    await RunSimulatedAssistantAsync(config);
                    return true;
                }
            }

            private static async Task RunSimulatedAssistantAsync(AgentConfig config)
            {
                Console.WriteLine("\n🤖 模擬 Assistant 已啟動 (無需 API 金鑰)");
                Console.WriteLine("💭 支援的查詢類型:");
                Console.WriteLine("   • 員工資料查詢 (如: 顯示員工資料)");
                Console.WriteLine("   • 天氣查詢 (如: 台北天氣)");
                Console.WriteLine("   • 客戶服務 (如: 客戶問題)");
                Console.WriteLine("   • 訂單管理 (如: 查詢訂單)");
                Console.WriteLine("輸入 'menu' 回主選單");
                
                Console.Write("\nAssistant > ");
                string? input;
                while ((input = Console.ReadLine()) is not null)
                {
                    if (input.Equals("menu", StringComparison.OrdinalIgnoreCase))
                        return;
                        
                    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                        return;

                    // Simple simulation responses
                    var response = GenerateSimulatedResponse(input);
                    Console.WriteLine($"\n🤖 Assistant: {response}\n");
                    Console.Write("Assistant > ");
                }
            }

            private static string GenerateSimulatedResponse(string input)
            {
                var lowerInput = input.ToLower();
                
                if (lowerInput.Contains("員工") || lowerInput.Contains("hr"))
                {
                    return @"📋 員工資料查詢結果 (模擬):
🔹 張小明 - 工程師 - 台北辦公室
🔹 李小華 - 設計師 - 新北辦公室  
🔹 王小美 - 專案經理 - 台北辦公室
📊 總計 3 位員工";
                }
                
                if (lowerInput.Contains("天氣") || lowerInput.Contains("weather"))
                {
                    return @"🌤️ 天氣查詢結果 (模擬):
📍 台北市: 25°C, 多雲
📍 新北市: 23°C, 小雨
🕐 更新時間: " + DateTime.Now.ToString("HH:mm");
                }
                
                if (lowerInput.Contains("客戶") || lowerInput.Contains("customer"))
                {
                    return @"👥 客戶服務回應 (模擬):
✅ 已為您查詢相關資訊
📞 如需進一步協助，請聯繫客服專線
📧 或發送郵件至客服信箱";
                }
                
                if (lowerInput.Contains("訂單") || lowerInput.Contains("order"))
                {
                    return @"📦 訂單查詢結果 (模擬):
🔸 訂單 #12345 - 處理中
🔸 訂單 #12346 - 已出貨
🔸 訂單 #12347 - 已完成
📊 總計 3 筆訂單";
                }
                
                return $"💭 已收到您的查詢: \"{input}\"\n🔄 這是模擬回應，實際功能需要設定 OpenAI API 金鑰";
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

            /// <summary>
            /// 獲取代理人類型，用於 FunctionChoiceBehavior 策略選擇
            /// </summary>
            protected virtual AgentType GetAgentType() => AgentType;

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
                var kernel = KernelFactory.CreateKernel();

                ConfigurePlugins(kernel);

                // 獲取可用函數列表用於智能策略判斷
                var availableFunctions = kernel.Plugins
                    .SelectMany(plugin => plugin.Select(function => $"{plugin.Name}.{function.Name}"))
                    .ToList();

                // 使用 FunctionChoiceBehaviorManager 創建最佳化設定
                var settings = FunctionChoiceBehaviorManager.CreateOptimizedSettings(
                    GetAgentType(), 
                    userInput: null, 
                    availableFunctions: availableFunctions
                );

                Console.WriteLine($"[Info] 🎯 使用函數選擇策略: {FunctionChoiceBehaviorManager.GetStrategyDescription(FunctionChoiceBehaviorManager.GetRecommendedStrategy(GetAgentType()))}");

                var history = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
                history.AddDeveloperMessage(SystemPrompt);

                // Debug: 顯示初始化的對話歷史
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("[Debug] 初始化對話歷史:");
                Console.WriteLine($"系統提示: {SystemPrompt}");
                Console.WriteLine($"可用函數數量: {availableFunctions.Count}");
                Console.WriteLine($"可用函數: {string.Join(", ", availableFunctions)}");
                Console.ResetColor();

                var chatService = kernel.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>();

                Console.WriteLine(UIConfig.ReadyMessage);
                Console.Write(UIConfig.InputPrompt);

                return await ProcessConversationLoop(chatService, history, settings, kernel, availableFunctions);
            }

            private async Task<bool> ProcessConversationLoop(
                Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService chatService,
                Microsoft.SemanticKernel.ChatCompletion.ChatHistory history,
                Microsoft.SemanticKernel.Connectors.OpenAI.OpenAIPromptExecutionSettings settings,
                Microsoft.SemanticKernel.Kernel kernel,
                List<string> availableFunctions)
            {
                string? input;
                while ((input = Console.ReadLine()) is not null)
                {
                    // 檢查退出指令：改為只結束當前代理人，不退出整個程式
                    if (input.Equals(_config.UI.Common.ExitCommand, StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("🔄 結束當前代理人，繼續執行下一個代理人...");
                        return true; // 改為回到主選單/繼續下一個代理人
                    }
                    
                    if (input.Equals(_config.UI.Common.BackToMenuCommand, StringComparison.OrdinalIgnoreCase))
                        return true; // 回到主選單

                    // 特殊指令：查看對話歷史
                    if (input.Equals("history", StringComparison.OrdinalIgnoreCase))
                    {
                        ShowChatHistory(history);
                        Console.Write(UIConfig.InputPrompt);
                        continue;
                    }

                    // 新增特殊指令：強制退出整個程式
                    if (input.Equals("quit", StringComparison.OrdinalIgnoreCase) || 
                        input.Equals("終止程式", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("🚪 強制退出整個程式...");
                        return false; // 退出整個程式
                    }

                    history.AddUserMessage(input);

                    // 🎯 根據用戶輸入動態調整 Function Choice Behavior 策略
                    var dynamicSettings = FunctionChoiceBehaviorManager.CreateOptimizedSettings(
                        GetAgentType(), 
                        userInput: input, 
                        availableFunctions: availableFunctions
                    );

                    Console.WriteLine($"[Info] 🧠 智能策略判斷: {FunctionChoiceBehaviorManager.GetStrategyDescription(FunctionChoiceBehaviorManager.GetStrategyForScenario(input))}");

                    var result = chatService.GetStreamingChatMessageContentsAsync(history, dynamicSettings, kernel: kernel);

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
                    Console.WriteLine("請選擇要執行的服務：");
                    foreach (var option in config.UI.MainMenu.Options)
                    {
                        Console.WriteLine($"{option.Key}. {option.Value}");
                    }
                    
                    Console.Write("請輸入選項 (1-6): ");
                    var input = Console.ReadLine();
                    var choice = input?.Trim() ?? "";
                    
                    if (string.IsNullOrWhiteSpace(choice))
                    {
                        Console.WriteLine($"\n{config.UI.MainMenu.InvalidNumber}");
                        continue;
                    }

                    // 檢查是否為退出命令
                    if (choice == "6" || choice.ToLower() == "exit")
                    {
                        Console.WriteLine($"\n{config.UI.MainMenu.GoodBye}");
                        return;
                    }

                    // 解析單選選項
                    if (!int.TryParse(choice, out int selectedOption) || selectedOption < 1 || selectedOption > 5)
                    {
                        Console.WriteLine($"\n⚠️ 無效的選項：'{choice}'");
                        Console.WriteLine("請輸入 1-5 之間的數字 (6=退出)。");
                        continue;
                    }

                    // 執行單一代理人
                    bool backToMenu = await MenuHelper.ExecuteSingleAgent(selectedOption, config);

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
                AddPluginIfNotExists(kernel, () => new CustomerServicePlugin(), "CustomerService");
                AddPluginIfNotExists(kernel, () => new OrderManagementPlugin(), "OrderManagement");
            }

            public static void ConfigureForWeatherService(Microsoft.SemanticKernel.Kernel kernel)
            {
                AddPluginIfNotExists(kernel, () => new WeatherServicePlugin(), "WeatherService");
            }

            public static void ConfigureForHRService(Microsoft.SemanticKernel.Kernel kernel)
            {
                AddPluginIfNotExists(kernel, () => new HRManagementPlugin(), "HRManagement");
            }

            public static void ConfigureForOrderManagement(Microsoft.SemanticKernel.Kernel kernel)
            {
                AddPluginIfNotExists(kernel, () => new OrderManagementPlugin(), "OrderManagement");
            }
            
            private static void AddPluginIfNotExists<T>(Microsoft.SemanticKernel.Kernel kernel, Func<T> createPlugin, string pluginName)
            {
                if (!kernel.Plugins.Any(p => p.Name == pluginName))
                {
                    kernel.Plugins.AddFromObject(createPlugin(), pluginName);
                }
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

        // 工具類別：選單處理工具
        public static class MenuHelper
        {
            // AI 智能助手模式處理 - 包含子選單循環
            public static async Task<bool> HandleAIAssistantMode(AgentConfig config)
            {
                while (true)
                {
                    Console.WriteLine("\n🎯 AI 智能助手模式");
                    Console.WriteLine("💡 功能選項：");
                    Console.WriteLine("   1️⃣ 直接對話 - 使用 OpenAI Assistant API");
                    Console.WriteLine("   2️⃣ 智能分析 - 自動選擇最適合的專業代理人");
                    Console.WriteLine("   3️⃣ 回到主選單");
                    Console.Write("\n請選擇模式 (1-3): ");
                    
                    var aiChoice = Console.ReadLine()?.Trim();
                    bool shouldContinue = false;
                    
                    switch (aiChoice)
                    {
                        case "1":
                            shouldContinue = await AgentManager.RunOpenAIAssistantAgent(config);
                            break;
                        case "2":
                            shouldContinue = await HandleAIMode(config);
                            break;
                        case "3":
                            return true; // 回到主選單
                        default:
                            Console.WriteLine("❌ 請輸入有效選項 (1-3)");
                            continue;
                    }
                    
                    // 如果子功能返回 true，表示要回到主選單
                    if (shouldContinue)
                    {
                        Console.WriteLine("\n🔄 返回 AI 智能助手模式選單...");
                        continue; // 繼續顯示 AI 智能助手模式選單
                    }
                    else
                    {
                        return false; // 退出整個程式
                    }
                }
            }

            // AI 智能模式處理
            public static async Task<bool> HandleAIMode(AgentConfig config)
            {
                Console.WriteLine("\n🤖 === AI 智能模式 ===");
                Console.WriteLine("💡 請描述您的需求，AI 將分析並推薦最適合的執行方案");
                Console.WriteLine("\n範例需求：");
                Console.WriteLine("• 我要查詢客戶 ABC123 的訂單狀態和天氣資訊");
                Console.WriteLine("• 幫我處理員工請假申請，然後查詢相關客戶服務記錄");
                Console.WriteLine("• 我需要完整的業務報告：包含訂單、客戶和人資資訊");
                Console.WriteLine();
                
                Console.Write("🎯 請描述您的需求: ");
                var userRequest = Console.ReadLine()?.Trim();
                
                if (string.IsNullOrWhiteSpace(userRequest))
                {
                    Console.WriteLine("❌ 需求描述不能為空，返回主選單");
                    return true;
                }

                Console.WriteLine("\n🔍 AI 正在分析您的需求...");
                var schedulingResult = await AIScheduler.AnalyzeAndSchedule(userRequest, config);
                
                if (!schedulingResult.Success)
                {
                    Console.WriteLine($"❌ {schedulingResult.Analysis}");
                    Console.WriteLine("🔄 請返回主選單手動選擇代理人");
                    return true;
                }

                // 顯示 AI 分析結果
                Console.WriteLine($"\n📊 {schedulingResult.Analysis}");
                Console.WriteLine($"\n🎯 AI 推薦執行計劃 ({schedulingResult.Plan.ExecutionMode} 模式):");
                
                for (int i = 0; i < schedulingResult.Plan.Steps.Count; i++)
                {
                    var step = schedulingResult.Plan.Steps[i];
                    var agentName = GetAgentTypeDisplayName(step.AgentType);
                    Console.WriteLine($"  {i + 1}. {agentName}");
                    Console.WriteLine($"     💭 原因: {step.Reason}");
                    if (step.ExpectedInputs.Any())
                    {
                        Console.WriteLine($"     📝 預期輸入: {string.Join(", ", step.ExpectedInputs)}");
                    }
                    if (!string.IsNullOrEmpty(step.Context))
                    {
                        Console.WriteLine($"     🔍 背景: {step.Context}");
                    }
                    Console.WriteLine();
                }

                if (schedulingResult.Recommendations.Any())
                {
                    Console.WriteLine("💡 AI 建議:");
                    foreach (var recommendation in schedulingResult.Recommendations)
                    {
                        Console.WriteLine($"   • {recommendation}");
                    }
                    Console.WriteLine();
                }

                // 提供執行模式選擇
                Console.WriteLine("🚀 執行模式選擇：");
                Console.WriteLine("1. 🎯 一體化智能回應（AI 直接整合所有功能並提供完整答案）");
                Console.WriteLine("2. 📋 分步執行模式（依序進入各個代理人進行互動）");
                Console.WriteLine("3. 🔄 返回主選單");
                Console.Write("請選擇執行模式 (1/2/3，預設為1): ");
                
                var modeChoice = Console.ReadLine()?.Trim();
                
                if (modeChoice == "3")
                {
                    Console.WriteLine("🔄 返回主選單");
                    return true;
                }
                else if (modeChoice == "2")
                {
                    // 原有的分步執行模式
                    Console.WriteLine("\n🚀 開始分步執行 AI 推薦的執行計劃...");
                    return await ExecuteAIPlan(schedulingResult.Plan, config);
                }
                else
                {
                    // 新的一體化智能回應模式
                    Console.WriteLine("\n🎯 一體化智能回應模式啟動...");
                    return await ExecuteIntegratedAIResponse(userRequest, schedulingResult.Plan, config);
                }
            }

            // 執行一體化 AI 回應
            private static async Task<bool> ExecuteIntegratedAIResponse(string userRequest, AIExecutionPlan plan, AgentConfig config)
            {
                Console.WriteLine("🤖 AI 正在整合所有相關資料並生成完整回應...");
                Console.WriteLine($"📋 需要調用 {plan.Steps.Count} 個專業模組的資料");
                
                var kernel = KernelFactory.CreateKernel();

                // 收集所有需要的代理人類型，避免重複配置
                var requiredAgentTypes = plan.Steps.Select(s => s.AgentType).Distinct().ToList();
                
                // 一次性配置所有需要的插件
                foreach (var agentType in requiredAgentTypes)
                {
                    Console.WriteLine($"🔍 正在配置{GetAgentTypeDisplayName(agentType)}的功能模組...");
                    
                    switch (agentType)
                    {
                        case AgentType.CustomerService:
                            PluginManager.ConfigureForCustomerService(kernel);
                            break;
                        case AgentType.WeatherService:
                            PluginManager.ConfigureForWeatherService(kernel);
                            break;
                        case AgentType.HRManagement:
                            PluginManager.ConfigureForHRService(kernel);
                            break;
                        case AgentType.OrderManagement:
                            PluginManager.ConfigureForOrderManagement(kernel);
                            break;
                    }
                }

                // 獲取所有可用函數用於智能策略判斷和調試
                var availableFunctions = kernel.Plugins
                    .SelectMany(plugin => plugin.Select(function => $"{plugin.Name}.{function.Name}"))
                    .ToList();

                Console.WriteLine($"[Debug] 🔧 已載入的函數: {string.Join(", ", availableFunctions)}");

                // 構建更明確的整合系統提示
                var integratedPrompt = $@"你是一個整合型 AI 助理，具備客戶服務、訂單管理、天氣預報、人力資源等多項專業能力。
你必須根據用戶需求，主動調用相關的函數工具來獲取最新的真實資料。

用戶需求：{userRequest}

重要指示：
1. 對於天氣查詢（台北、高雄、台中、台南等）：必須調用 QueryWeather 函數獲取真實天氣資料
2. 對於員工查詢（顯示員工、查詢員工等）：必須調用 QueryEmployees 函數獲取真實員工資料  
3. 對於客戶查詢：必須調用 GetCustomerInfo 或 QueryCustomers 函數獲取真實客戶資料
4. 對於訂單查詢：必須調用 GetOrderStatus 或 QueryOrders 函數獲取真實訂單資料

請務必先調用相關函數獲取資料，然後基於實際資料提供完整、專業的回應。
如果沒有調用函數就回應，那是錯誤的行為。
使用繁體中文回應，格式要清晰易讀。";

                var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
                chatHistory.AddSystemMessage(integratedPrompt);
                chatHistory.AddUserMessage(userRequest);

                // 🎯 強制使用 Required 策略確保函數調用
                var settings = new OpenAIPromptExecutionSettings
                {
                    MaxTokens = 2000,
                    Temperature = 0.7f,
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Required() // 強制調用函數
                };

                Console.WriteLine($"[Info] 🧠 整合模式策略: ⚡ 必需調用 - 強制調用函數獲取數據");
                Console.WriteLine($"[Info] 💡 可用函數總數: {availableFunctions.Count} 個");

                var chatService = kernel.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>();
                
                Console.WriteLine("\n" + new string('=', 80));
                Console.WriteLine("🎯 AI 整合回應結果：");
                Console.WriteLine(new string('=', 80));

                try
                {
                    // 使用非流式回應以便更好地處理函數調用
                    var response = await chatService.GetChatMessageContentAsync(chatHistory, settings, kernel);
                    
                    Console.WriteLine($"[Debug] 📤 模型回應: {response.Content}");
                    Console.WriteLine(response.Content);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ 整合回應執行錯誤: {ex.Message}");
                    Console.WriteLine($"[Debug] 錯誤詳情: {ex}");
                    
                    // 備用：使用流式回應
                    Console.WriteLine("\n🔄 嘗試使用備用回應方式...");
                    var fallbackResult = chatService.GetStreamingChatMessageContentsAsync(chatHistory, settings, kernel: kernel);
                    
                    string fallbackResponse = "";
                    await foreach (var content in fallbackResult)
                    {
                        if (content.Content != null)
                        {
                            Console.Write(content.Content);
                            fallbackResponse += content.Content;
                        }
                    }
                    Console.WriteLine($"\n[Debug] 📤 備用回應: {fallbackResponse}");
                }
                
                Console.WriteLine();
                Console.WriteLine(new string('=', 80));
                Console.WriteLine("✅ AI 整合回應完成");
                Console.WriteLine();
                
                // 提供後續選項
                Console.WriteLine("🎯 後續選項：");
                Console.WriteLine("1. 🔄 詢問相關問題");
                Console.WriteLine("2. 🏠 返回主選單");
                Console.Write("請選擇 (1/2，預設為2): ");
                
                var followUpChoice = Console.ReadLine()?.Trim();
                
                if (followUpChoice == "1")
                {
                    return await HandleFollowUpQuestions(kernel, chatHistory, config);
                }
                else
                {
                    Console.WriteLine("🏠 返回主選單");
                    return true;
                }
            }

            // 處理後續問題
            private static async Task<bool> HandleFollowUpQuestions(Microsoft.SemanticKernel.Kernel kernel, Microsoft.SemanticKernel.ChatCompletion.ChatHistory chatHistory, AgentConfig config)
            {
                var chatService = kernel.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>();
                
                // 獲取所有可用函數用於智能策略判斷
                var availableFunctions = kernel.Plugins
                    .SelectMany(plugin => plugin.Select(function => $"{plugin.Name}.{function.Name}"))
                    .ToList();

                Console.WriteLine($"[Info] 💡 後續對話模式啟用，可用函數: {availableFunctions.Count} 個");
                
                Console.WriteLine("\n💬 繼續對話模式（輸入 'back' 返回選項，'menu' 返回主選單）:");
                
                while (true)
                {
                    Console.Write("\n您: ");
                    var input = Console.ReadLine()?.Trim();
                    
                    if (string.IsNullOrWhiteSpace(input))
                        continue;
                        
                    if (input.ToLower() == "back")
                    {
                        Console.WriteLine("🔄 返回選項選擇");
                        return true;
                    }
                    
                    if (input.ToLower() == "menu")
                    {
                        Console.WriteLine("🏠 返回主選單");
                        return true;
                    }
                    
                    chatHistory.AddUserMessage(input);
                    
                    // 🎯 根據每個後續問題動態調整 Function Choice Behavior
                    var dynamicSettings = FunctionChoiceBehaviorManager.CreateOptimizedSettings(
                        AgentType.CustomerService, // 預設使用客戶服務策略，因為是綜合模式
                        userInput: input, 
                        availableFunctions: availableFunctions
                    );

                    Console.WriteLine($"[Info] � 使用策略: {FunctionChoiceBehaviorManager.GetStrategyForScenario(input).ToString()}");
                    
                    Console.Write("�🤖 AI: ");
                    var result = chatService.GetStreamingChatMessageContentsAsync(chatHistory, dynamicSettings, kernel: kernel);
                    
                    string response = "";
                    await foreach (var content in result)
                    {
                        if (content.Content != null)
                        {
                            Console.Write(content.Content);
                            response += content.Content;
                        }
                    }
                    Console.WriteLine();
                    
                    chatHistory.AddAssistantMessage(response);
                }
            }

            // 執行 AI 計劃
            private static async Task<bool> ExecuteAIPlan(AIExecutionPlan plan, AgentConfig config)
            {
                int currentStep = 0;
                int totalSteps = plan.Steps.Count;
                
                Console.WriteLine($"\n🎯 AI 執行模式：{plan.ExecutionMode}");
                Console.WriteLine($"📋 總共 {totalSteps} 個執行步驟");
                Console.WriteLine("💡 特殊指令：");
                Console.WriteLine("   • 輸入 'menu' 立即返回主選單");
                Console.WriteLine("   • 輸入 'exit' 結束當前代理人並繼續下一個");
                Console.WriteLine("   • 輸入 'quit' 強制退出整個程式");
                Console.WriteLine("   • 輸入 'history' 查看對話歷史");
                
                foreach (var step in plan.Steps)
                {
                    currentStep++;
                    
                    // 顯示當前執行進度
                    Console.WriteLine($"\n{new string('=', 60)}");
                    Console.WriteLine($"🤖 AI 步驟 [{currentStep}/{totalSteps}]: {GetAgentTypeDisplayName(step.AgentType)}");
                    Console.WriteLine($"💭 執行原因: {step.Reason}");
                    if (!string.IsNullOrEmpty(step.Context))
                    {
                        Console.WriteLine($"🔍 背景資訊: {step.Context}");
                    }
                    if (step.ExpectedInputs.Any())
                    {
                        Console.WriteLine($"📝 建議輸入: {string.Join(", ", step.ExpectedInputs)}");
                    }
                    Console.WriteLine($"{new string('=', 60)}");
                    
                    // 顯示代理人啟動訊息
                    var agentUIConfig = GetAgentUIConfig(step.AgentType, config);
                    if (!string.IsNullOrEmpty(agentUIConfig.StartMessage))
                    {
                        Console.WriteLine(agentUIConfig.StartMessage);
                    }
                    
                    // 執行代理人
                    bool agentResult = await AgentManager.RunAgent(step.AgentType, config);
                    
                    // 處理代理人執行結果
                    if (!agentResult)
                    {
                        Console.WriteLine($"\n🚪 強制退出整個程式");
                        return false;
                    }
                    else
                    {
                        Console.WriteLine($"\n✅ AI 步驟 {currentStep} 完成：{GetAgentTypeDisplayName(step.AgentType)}");
                        
                        // 如果還有其他步驟，詢問是否繼續
                        if (currentStep < totalSteps)
                        {
                            Console.WriteLine($"📋 AI 計劃還有 {totalSteps - currentStep} 個步驟未執行");
                            Console.WriteLine("\n選擇：");
                            Console.WriteLine("1. 繼續執行 AI 計劃（推薦）");
                            Console.WriteLine("2. 回到主選單（停止 AI 執行）");
                            Console.Write("請選擇 (1/2，預設為1): ");
                            
                            var continueChoice = Console.ReadLine()?.Trim();
                            if (continueChoice == "2")
                            {
                                Console.WriteLine("🔄 停止 AI 執行計劃，回到主選單");
                                return true;
                            }
                            else
                            {
                                Console.WriteLine("✅ 繼續執行 AI 計劃...");
                                continue;
                            }
                        }
                    }
                }
                
                // 所有 AI 步驟都執行完成
                Console.WriteLine($"\n🎉 AI 執行計劃完成！所有 {totalSteps} 個步驟都已執行完畢");
                Console.WriteLine("🔄 返回主選單");
                return true;
            }

            // 獲取代理人類型顯示名稱
            private static string GetAgentTypeDisplayName(AgentType agentType)
            {
                return agentType switch
                {
                    AgentType.CustomerService => "客戶服務專員",
                    AgentType.WeatherService => "天氣預報專員",
                    AgentType.HRManagement => "人力資源專員",
                    AgentType.OrderManagement => "訂單管理專員",
                    AgentType.OpenAIAssistant => "OpenAI Assistant 整合專員",
                    _ => "未知代理人"
                };
            }

            // 獲取代理人 UI 配置
            private static AgentUIConfig GetAgentUIConfig(AgentType agentType, AgentConfig config)
            {
                return agentType switch
                {
                    AgentType.CustomerService => config.UI.Agents.CustomerService,
                    AgentType.WeatherService => config.UI.Agents.WeatherService,
                    AgentType.HRManagement => config.UI.Agents.HRManagement,
                    AgentType.OrderManagement => config.UI.Agents.OrderManagement,
                    AgentType.OpenAIAssistant => config.UI.Agents.OpenAIAssistant,
                    _ => new AgentUIConfig()
                };
            }

            // 從配置中獲取代理人名稱
            public static string GetAgentName(string optionKey, AgentConfig config)
            {
                if (config?.UI?.MainMenu?.Options != null && 
                    config.UI.MainMenu.Options.ContainsKey(optionKey))
                {
                    return config.UI.MainMenu.Options[optionKey];
                }
                return $"選項 {optionKey}";
            }
            
            // 執行單一代理人
            public static async Task<bool> ExecuteSingleAgent(int agentOption, AgentConfig config)
            {
                Console.WriteLine($"\n🚀 啟動：{GetAgentName(agentOption, config)}");
                
                bool backToMenu = true;
                
                switch (agentOption)
                {
                    case 1:
                        Console.WriteLine(config.UI.Agents.CustomerService.StartMessage);
                        backToMenu = await AgentManager.RunAgent(AgentType.CustomerService, config);
                        break;
                    case 2:
                        Console.WriteLine(config.UI.Agents.WeatherService.StartMessage);
                        backToMenu = await AgentManager.RunAgent(AgentType.WeatherService, config);
                        break;
                    case 3:
                        Console.WriteLine(config.UI.Agents.HRManagement.StartMessage);
                        backToMenu = await AgentManager.RunAgent(AgentType.HRManagement, config);
                        break;
                    case 4:
                        Console.WriteLine(config.UI.Agents.OrderManagement.StartMessage);
                        backToMenu = await AgentManager.RunAgent(AgentType.OrderManagement, config);
                        break;
                    case 5:
                        Console.WriteLine(config.UI.Agents.OpenAIAssistant.StartMessage);
                        backToMenu = await MenuHelper.HandleAIAssistantMode(config);
                        break;
                    default:
                        Console.WriteLine($"❌ 無效的選項: {agentOption}");
                        return true;
                }
                
                return backToMenu;
            }

            // 直接執行代理人
            public static async Task<bool> ExecuteAgentsDirectly(List<int> selectedOptions, AgentConfig config)
            {
                var agents = new List<int>();
                
                // 直接添加選項到代理人列表
                agents.AddRange(selectedOptions);

                if (agents.Count == 0)
                {
                    Console.WriteLine("⚠️ 沒有有效的代理人可執行");
                    return true;
                }

                Console.WriteLine($"\n🚀 即將執行 {agents.Count} 個代理人：");
                foreach (var option in agents)
                {
                    var name = GetAgentName(option, config);
                    Console.WriteLine($"   📋 {name}");
                }
                
                // 如果有多個代理人，詢問執行順序
                if (agents.Count > 1)
                {
                    Console.WriteLine("\n🎯 順序執行模式（多代理人智能切換）");
                    Console.WriteLine("💡 功能特色：");
                    Console.WriteLine("   ✅ 依序啟動每個代理人");
                    Console.WriteLine("   ✅ 隨時輸入 'menu' 切換到其他助手");
                    Console.WriteLine("   ✅ 隨時輸入 'exit' 結束當前代理人並繼續下一個");
                    Console.WriteLine("   ✅ 隨時輸入 'quit' 強制退出整個程式");
                    Console.WriteLine("   ✅ 隨時輸入 'history' 查看對話歷史");
                    Console.WriteLine("   ✅ 中途停止並選擇是否繼續剩餘助手");
                    
                    Console.Write("\n是否要調整執行順序？(y/N): ");
                    var adjustOrder = Console.ReadLine()?.Trim().ToLower();
                    if (adjustOrder == "y" || adjustOrder == "yes")
                    {
                        agents = AskExecutionOrder(agents, config);
                    }
                }

                // 執行選中的代理人（順序執行）
                return await ExecuteSelectedAgents(agents, config, true, "sequential");
            }

            // 獲取代理人名稱的輔助方法
            private static string GetAgentName(int option, AgentConfig config)
            {
                return option switch
                {
                    1 => "客戶服務專員",
                    2 => "天氣預報專員", 
                    3 => "人力資源專員",
                    4 => "訂單管理專員",
                    _ => "未知代理人"
                };
            }
            

            // 解析選項字串（支援單選和複選）
            public static List<int> ParseSelectedOptions(string input, int maxOption = 5)
            {
                var selectedOptions = new List<int>();
                if (string.IsNullOrWhiteSpace(input))
                    return selectedOptions;

                // 移除空白字元並分割
                var parts = input.Replace(" ", "").Split(',');
                
                foreach (var part in parts)
                {
                    if (int.TryParse(part.Trim(), out int option))
                    {
                        if (option >= 1 && option <= maxOption && !selectedOptions.Contains(option))
                        {
                            selectedOptions.Add(option);
                        }
                    }
                }
                
                return selectedOptions;
            }

            // 詢問執行順序
            public static List<int> AskExecutionOrder(List<int> selectedOptions, AgentConfig config)
            {
                if (selectedOptions.Count <= 1)
                    return selectedOptions;

                Console.WriteLine($"\n{config.UI.MainMenu.ExecutionOrderPrompt}");
                
                // 顯示已選擇的代理人
                for (int i = 0; i < selectedOptions.Count; i++)
                {
                    var optionKey = selectedOptions[i].ToString();
                    var optionName = GetAgentName(optionKey, config);
                    Console.WriteLine($"{i + 1}. {optionName}");
                }
                
                Console.Write($"{config.UI.MainMenu.ExecutionOrderInput}");
                var orderInput = Console.ReadLine()?.Trim();
                
                // 解析執行順序
                var orderOptions = ParseSelectedOptions(orderInput ?? "", selectedOptions.Count);
                
                // 驗證順序是否完整且正確
                if (orderOptions.Count == selectedOptions.Count && 
                    orderOptions.All(o => o >= 1 && o <= selectedOptions.Count))
                {
                    var orderedList = new List<int>();
                    foreach (var order in orderOptions)
                    {
                        orderedList.Add(selectedOptions[order - 1]);
                    }
                    return orderedList;
                }
                else
                {
                    Console.WriteLine($"{config.UI.MainMenu.InvalidExecutionOrder}");
                    return selectedOptions; // 返回原始順序
                }
            }

            // 執行指定的代理人（順序執行）
            public static async Task<bool> ExecuteSelectedAgents(List<int> agentOptions, AgentConfig config, bool isFirstSelection = false, string executionMode = "sequential")
            {
                return await ExecuteAgentsSequentially(agentOptions, config, isFirstSelection);
            }
            
            // 順序執行代理人（增強版：智能切換和繼續選項）
            private static async Task<bool> ExecuteAgentsSequentially(List<int> agentOptions, AgentConfig config, bool isFirstSelection)
            {
                int currentIndex = 0;
                int totalAgents = agentOptions.Count;
                
                Console.WriteLine($"\n🎯 順序執行模式：將依序執行 {totalAgents} 個代理人");
                Console.WriteLine("💡 智能功能：");
                Console.WriteLine("   • 輸入 'menu' 立即切換到主選單");
                Console.WriteLine("   • 輸入 'exit' 結束當前代理人並繼續下一個");
                Console.WriteLine("   • 輸入 'quit' 強制退出整個程式");
                Console.WriteLine("   • 輸入 'history' 查看對話歷史");
                Console.WriteLine("   • 中途停止可選擇繼續剩餘助手");
                
                foreach (var option in agentOptions)
                {
                    currentIndex++;
                    
                    // 顯示當前執行進度
                    Console.WriteLine($"\n{new string('=', 50)}");
                    Console.WriteLine($"🚀 [{currentIndex}/{totalAgents}] 正在啟動：{GetAgentName(option, config)}");
                    Console.WriteLine($"{new string('=', 50)}");
                    
                    bool backToMenu = true;
                    
                    switch (option)
                    {
                        case 1:
                            Console.WriteLine(config.UI.Agents.CustomerService.StartMessage);
                            backToMenu = await AgentManager.RunAgent(AgentType.CustomerService, config);
                            break;
                        case 2:
                            Console.WriteLine(config.UI.Agents.WeatherService.StartMessage);
                            backToMenu = await AgentManager.RunAgent(AgentType.WeatherService, config);
                            break;
                        case 3:
                            Console.WriteLine(config.UI.Agents.HRManagement.StartMessage);
                            backToMenu = await AgentManager.RunAgent(AgentType.HRManagement, config);
                            break;
                        case 4:
                            Console.WriteLine(config.UI.Agents.OrderManagement.StartMessage);
                            backToMenu = await AgentManager.RunAgent(AgentType.OrderManagement, config);
                            break;
                        case 5:
                            Console.WriteLine(config.UI.Agents.OpenAIAssistant.StartMessage);
                            backToMenu = await AgentManager.RunOpenAIAssistantAgent(config);
                            break;
                        default:
                            Console.WriteLine($"❌ 無效的選項: {option}，跳過此代理人");
                            continue; // 繼續下一個選項
                    }
                    
                    // 處理代理人執行結果
                    if (!backToMenu)
                    {
                        // 代理人返回 false (強制退出整個程式)
                        Console.WriteLine($"\n� 強制退出整個程式");
                        return false; // 退出整個程式
                    }
                    else
                    {
                        // 代理人返回 true (結束當前代理人或回到主選單)
                        Console.WriteLine($"\n✅ 代理人 {GetAgentName(option, config)} 執行完成");
                        
                        // 如果還有其他代理人未執行，詢問是否繼續
                        if (currentIndex < totalAgents)
                        {
                            Console.WriteLine($"📋 還有 {totalAgents - currentIndex} 個代理人未執行：");
                            for (int i = currentIndex; i < agentOptions.Count; i++)
                            {
                                Console.WriteLine($"   - {GetAgentName(agentOptions[i], config)}");
                            }
                            Console.WriteLine("\n選擇：");
                            Console.WriteLine("1. 繼續執行下一個代理人（推薦）");
                            Console.WriteLine("2. 回到主選單（停止執行剩餘代理人）");
                            Console.Write("請選擇 (1/2，預設為1): ");
                            
                            var continueChoice = Console.ReadLine()?.Trim();
                            if (continueChoice == "2")
                            {
                                Console.WriteLine("🔄 回到主選單，停止執行剩餘代理人");
                                return true; // 回到主選單
                            }
                            else
                            {
                                Console.WriteLine("✅ 繼續執行下一個代理人...");
                                continue; // 繼續執行下一個代理人
                            }
                        }
                    }
                }
                
                // 所有代理人都執行完成
                Console.WriteLine($"\n🎉 所有 {totalAgents} 個代理人都執行完成！回到主選單");
                return true; 
            }
        }
    }




