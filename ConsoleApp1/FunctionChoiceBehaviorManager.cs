using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace day1
{
    /// <summary>
    /// Function Choice Behavior 智能管理器
    /// 提供自動、必需、禁用等不同策略的智能切換
    /// </summary>
    public static class FunctionChoiceBehaviorManager
    {
        /// <summary>
        /// 函數選擇策略
        /// </summary>
        public enum FunctionChoiceStrategy
        {
            /// <summary>
            /// 自動選擇 - AI 自動判斷是否需要調用函數
            /// </summary>
            Auto,
            
            /// <summary>
            /// 必需調用 - 強制 AI 必須調用至少一個函數
            /// </summary>
            Required,
            
            /// <summary>
            /// 禁用函數 - 禁止調用任何函數，純文字回應
            /// </summary>
            None,
            
            /// <summary>
            /// 智能選擇 - 根據用戶輸入智能判斷最佳策略
            /// </summary>
            Smart
        }

        /// <summary>
        /// 獲取 OpenAI 提示執行設定（包含 Function Choice Behavior）
        /// </summary>
        /// <param name="strategy">函數選擇策略</param>
        /// <param name="userInput">用戶輸入（用於智能策略判斷）</param>
        /// <param name="availableFunctions">可用函數列表（用於智能決策）</param>
        /// <returns>配置好的 OpenAI 提示執行設定</returns>
        public static OpenAIPromptExecutionSettings GetExecutionSettings(
            FunctionChoiceStrategy strategy = FunctionChoiceStrategy.Auto, 
            string? userInput = null,
            IEnumerable<string>? availableFunctions = null)
        {
            var settings = new OpenAIPromptExecutionSettings
            {
                MaxTokens = 2000,
                Temperature = 0.7f,
                TopP = 1.0f
            };

            // 根據策略設定 FunctionChoiceBehavior
            switch (strategy)
            {
                case FunctionChoiceStrategy.Auto:
                    settings.FunctionChoiceBehavior = FunctionChoiceBehavior.Auto();
                    break;

                case FunctionChoiceStrategy.Required:
                    settings.FunctionChoiceBehavior = FunctionChoiceBehavior.Required();
                    break;

                case FunctionChoiceStrategy.None:
                    settings.FunctionChoiceBehavior = FunctionChoiceBehavior.None();
                    break;

                case FunctionChoiceStrategy.Smart:
                    // 智能選擇策略 - 根據用戶輸入自動判斷最佳策略
                    var smartStrategy = DetermineSmartStrategy(userInput, availableFunctions);
                    settings.FunctionChoiceBehavior = GetFunctionChoiceBehavior(smartStrategy);
                    Console.WriteLine($"[Debug] 智能策略判斷結果: {smartStrategy}");
                    break;

                default:
                    settings.FunctionChoiceBehavior = FunctionChoiceBehavior.Auto();
                    break;
            }

            return settings;
        }

        /// <summary>
        /// 根據代理人類型獲取推薦的函數選擇策略
        /// </summary>
        /// <param name="agentType">代理人類型</param>
        /// <returns>推薦的函數選擇策略</returns>
        public static FunctionChoiceStrategy GetRecommendedStrategy(AgentType agentType)
        {
            // 先檢查配置文件中的設置
            try
            {
                var config = day1.AppSettings.FunctionChoice;
                if (config.AgentStrategies.TryGetValue(agentType.ToString(), out var configuredStrategy))
                {
                    if (Enum.TryParse<FunctionChoiceStrategy>(configuredStrategy, out var strategy))
                    {
                        return strategy;
                    }
                }
            }
            catch
            {
                // 配置讀取失敗時使用預設策略
            }

            // 預設策略
            return agentType switch
            {
                AgentType.CustomerService => FunctionChoiceStrategy.Auto,    // 客戶服務需要靈活調用
                AgentType.WeatherService => FunctionChoiceStrategy.Required, // 天氣服務通常需要查詢API
                AgentType.HRManagement => FunctionChoiceStrategy.Auto,       // 人資管理需要靈活處理
                AgentType.OrderManagement => FunctionChoiceStrategy.Auto,    // 訂單管理需要靈活調用
                _ => FunctionChoiceStrategy.Auto
            };
        }

        /// <summary>
        /// 根據用戶需求場景獲取最佳策略
        /// </summary>
        /// <param name="scenario">使用場景</param>
        /// <returns>最佳函數選擇策略</returns>
        public static FunctionChoiceStrategy GetStrategyForScenario(string scenario)
        {
            var lowerScenario = scenario.ToLower();
            
            // 需要強制調用函數的場景
            if (ContainsKeywords(lowerScenario, new[] { "查詢", "搜尋", "找", "取得", "獲取", "檢查", "確認" }))
            {
                return FunctionChoiceStrategy.Required;
            }
            
            // 純對話或解釋的場景
            if (ContainsKeywords(lowerScenario, new[] { "解釋", "說明", "介紹", "什麼是", "如何", "為什麼" }))
            {
                return FunctionChoiceStrategy.None;
            }
            
            // 其他情況使用智能自動判斷
            return FunctionChoiceStrategy.Smart;
        }

        /// <summary>
        /// 智能策略判斷 - 根據用戶輸入和可用函數決定最佳策略
        /// </summary>
        /// <param name="userInput">用戶輸入</param>
        /// <param name="availableFunctions">可用函數列表</param>
        /// <returns>判斷出的最佳策略</returns>
        private static FunctionChoiceStrategy DetermineSmartStrategy(string? userInput, IEnumerable<string>? availableFunctions)
        {
            if (string.IsNullOrWhiteSpace(userInput))
            {
                return FunctionChoiceStrategy.Auto;
            }

            var input = userInput.ToLower();
            var functions = availableFunctions?.ToList() ?? new List<string>();

            try
            {
                var config = day1.AppSettings.FunctionChoice;
                
                // 1. 明確的查詢或操作請求 - 使用 Required
                var queryKeywords = config.ScenarioStrategies.QueryKeywords.Any() 
                    ? config.ScenarioStrategies.QueryKeywords.ToArray()
                    : new[] { "查詢", "搜尋", "找", "取得", "獲取", "檢索", "查找", "調用", "執行" };
                    
                if (ContainsKeywords(input, queryKeywords) && config.ScenarioStrategies.ForceRequired)
                {
                    return FunctionChoiceStrategy.Required;
                }

                // 2. 純理論或解釋性問題 - 使用 None
                var explanationKeywords = config.ScenarioStrategies.ExplanationKeywords.Any()
                    ? config.ScenarioStrategies.ExplanationKeywords.ToArray()
                    : new[] { "解釋", "說明", "介紹", "什麼是", "如何理解", "原理", "概念", "定義" };
                    
                if (ContainsKeywords(input, explanationKeywords) && config.ScenarioStrategies.ForceNone)
                {
                    return FunctionChoiceStrategy.None;
                }
            }
            catch
            {
                // 配置讀取失敗時使用預設關鍵字
                var queryKeywords = new[] { "查詢", "搜尋", "找", "取得", "獲取", "檢索", "查找", "調用", "執行" };
                if (ContainsKeywords(input, queryKeywords))
                {
                    return FunctionChoiceStrategy.Required;
                }

                var explanationKeywords = new[] { "解釋", "說明", "介紹", "什麼是", "如何理解", "原理", "概念", "定義" };
                if (ContainsKeywords(input, explanationKeywords))
                {
                    return FunctionChoiceStrategy.None;
                }
            }

            // 3. 包含具體實體名稱（如客戶ID、訂單號等）- 使用 Required
            if (ContainsSpecificEntities(input))
            {
                return FunctionChoiceStrategy.Required;
            }

            // 4. 根據可用函數數量判斷
            if (functions.Count > 3)
            {
                // 函數較多時使用自動選擇，讓AI智能判斷
                return FunctionChoiceStrategy.Auto;
            }
            else if (functions.Count > 0)
            {
                // 函數較少時傾向於使用，但不強制
                return FunctionChoiceStrategy.Auto;
            }

            // 5. 預設使用自動策略
            return FunctionChoiceStrategy.Auto;
        }

        /// <summary>
        /// 檢查文本是否包含指定關鍵字
        /// </summary>
        private static bool ContainsKeywords(string text, string[] keywords)
        {
            return keywords.Any(keyword => text.Contains(keyword));
        }

        /// <summary>
        /// 檢查是否包含具體實體（ID、編號等）
        /// </summary>
        private static bool ContainsSpecificEntities(string input)
        {
            // 檢查是否包含常見的ID模式
            var patterns = new[]
            {
                @"\b[A-Z]\d{3,}\b",      // A001, B123 等
                @"\b\d{4,}\b",           // 4位數以上的數字
                @"[編訂客用戶]號",           // 包含"編號"、"訂號"等
                @"ID|id|編號|客戶|用戶|訂單" // 明確的實體指示詞
            };

            return patterns.Any(pattern => System.Text.RegularExpressions.Regex.IsMatch(input, pattern));
        }

        /// <summary>
        /// 將策略轉換為具體的 FunctionChoiceBehavior
        /// </summary>
        private static FunctionChoiceBehavior GetFunctionChoiceBehavior(FunctionChoiceStrategy strategy)
        {
            return strategy switch
            {
                FunctionChoiceStrategy.Auto => FunctionChoiceBehavior.Auto(),
                FunctionChoiceStrategy.Required => FunctionChoiceBehavior.Required(),
                FunctionChoiceStrategy.None => FunctionChoiceBehavior.None(),
                _ => FunctionChoiceBehavior.Auto()
            };
        }

        /// <summary>
        /// 獲取策略的中文描述
        /// </summary>
        /// <param name="strategy">函數選擇策略</param>
        /// <returns>中文描述</returns>
        public static string GetStrategyDescription(FunctionChoiceStrategy strategy)
        {
            return strategy switch
            {
                FunctionChoiceStrategy.Auto => "🤖 自動選擇 - AI 智能判斷是否需要調用函數",
                FunctionChoiceStrategy.Required => "⚡ 必需調用 - 強制調用至少一個函數獲取數據",
                FunctionChoiceStrategy.None => "💬 純對話模式 - 禁用函數調用，純文字回應",
                FunctionChoiceStrategy.Smart => "🧠 智能策略 - 根據輸入自動選擇最佳策略",
                _ => "🤖 自動選擇"
            };
        }

        /// <summary>
        /// 為特定代理人類型創建最佳化的執行設定
        /// </summary>
        /// <param name="agentType">代理人類型</param>
        /// <param name="userInput">用戶輸入</param>
        /// <param name="availableFunctions">可用函數</param>
        /// <returns>最佳化的執行設定</returns>
        public static OpenAIPromptExecutionSettings CreateOptimizedSettings(
            AgentType agentType, 
            string? userInput = null, 
            IEnumerable<string>? availableFunctions = null)
        {
            // 先獲取代理人推薦策略
            var recommendedStrategy = GetRecommendedStrategy(agentType);
            
            // 如果有用戶輸入，進一步智能優化
            if (!string.IsNullOrWhiteSpace(userInput))
            {
                var smartStrategy = DetermineSmartStrategy(userInput, availableFunctions);
                
                // 結合推薦策略和智能判斷
                var finalStrategy = CombineStrategies(recommendedStrategy, smartStrategy);
                
                return GetExecutionSettings(finalStrategy, userInput, availableFunctions);
            }
            
            return GetExecutionSettings(recommendedStrategy, userInput, availableFunctions);
        }

        /// <summary>
        /// 結合兩種策略，選擇最適合的
        /// </summary>
        private static FunctionChoiceStrategy CombineStrategies(FunctionChoiceStrategy recommended, FunctionChoiceStrategy smart)
        {
            // Required 優先級最高
            if (recommended == FunctionChoiceStrategy.Required || smart == FunctionChoiceStrategy.Required)
            {
                return FunctionChoiceStrategy.Required;
            }
            
            // None 次之
            if (recommended == FunctionChoiceStrategy.None || smart == FunctionChoiceStrategy.None)
            {
                return FunctionChoiceStrategy.None;
            }
            
            // 其他情況使用 Auto
            return FunctionChoiceStrategy.Auto;
        }

        /// <summary>
        /// 顯示函數選擇策略的幫助信息
        /// </summary>
        public static void DisplayStrategyHelp()
        {
            Console.WriteLine("\n🎯 === Function Choice Behavior 策略說明 ===");
            Console.WriteLine(GetStrategyDescription(FunctionChoiceStrategy.Auto));
            Console.WriteLine(GetStrategyDescription(FunctionChoiceStrategy.Required));
            Console.WriteLine(GetStrategyDescription(FunctionChoiceStrategy.None));
            Console.WriteLine(GetStrategyDescription(FunctionChoiceStrategy.Smart));
            Console.WriteLine("\n💡 系統會根據代理人類型和用戶輸入自動選擇最佳策略");
        }
    }
}