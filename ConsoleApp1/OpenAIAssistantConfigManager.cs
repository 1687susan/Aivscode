using System;
using System.IO;
using System.Text.Json;

namespace day1
{
    /// <summary>
    /// OpenAI Assistant 配置類
    /// </summary>
    public class OpenAIAssistantConfig
    {
        public OpenAIAssistantSettings OpenAIAssistant { get; set; } = new();
    }

    public class OpenAIAssistantSettings
    {
        public OpenAIAssistantMessages Messages { get; set; } = new();
        public string SystemPrompt { get; set; } = string.Empty;
        public string AssistantName { get; set; } = string.Empty;
        public OpenAIAssistantCommands Commands { get; set; } = new();
        public OpenAIAssistantCapabilities Capabilities { get; set; } = new();
    }

    public class OpenAIAssistantMessages
    {
        public string Initializing { get; set; } = string.Empty;
        public string AssistantCreated { get; set; } = string.Empty;
        public string InitializationFailed { get; set; } = string.Empty;
        public string ThreadCreated { get; set; } = string.Empty;
        public string ProcessingRequest { get; set; } = string.Empty;
        public string RequestError { get; set; } = string.Empty;
        public string EndCurrentAgent { get; set; } = string.Empty;
        public string NewConversationCreated { get; set; } = string.Empty;
        public string NewConversationFailed { get; set; } = string.Empty;
        public string NoActiveThread { get; set; } = string.Empty;
        public string CurrentThread { get; set; } = string.Empty;
        public string ProcessingFailed { get; set; } = string.Empty;
        public string ExecutionError { get; set; } = string.Empty;
        public string AssistantResponse { get; set; } = string.Empty;
    }

    public class OpenAIAssistantCommands
    {
        public string New { get; set; } = "new";
        public string Capabilities { get; set; } = "capabilities";
    }

    public class OpenAIAssistantCapabilities
    {
        public string Title { get; set; } = string.Empty;
        public string CoreAbilities { get; set; } = string.Empty;
        public string[] CoreAbilitiesList { get; set; } = Array.Empty<string>();
        public string IntegratedServices { get; set; } = string.Empty;
        public string[] IntegratedServicesList { get; set; } = Array.Empty<string>();
        public string Usage { get; set; } = string.Empty;
        public string[] UsageList { get; set; } = Array.Empty<string>();
        public string TechnicalFeatures { get; set; } = string.Empty;
        public string[] TechnicalFeaturesList { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// OpenAI Assistant 配置管理器
    /// </summary>
    public static class OpenAIAssistantConfigManager
    {
        private static OpenAIAssistantConfig? _config;
        private const string ConfigFileName = "openai-assistant-config.json";

        /// <summary>
        /// 載入配置
        /// </summary>
        public static OpenAIAssistantConfig LoadConfig()
        {
            if (_config != null)
                return _config;

            try
            {
                var configPath = GetConfigPath();
                if (!File.Exists(configPath))
                {
                    Console.WriteLine($"[Warning] OpenAI Assistant 配置文件不存在: {configPath}");
                    return new OpenAIAssistantConfig();
                }

                var json = File.ReadAllText(configPath);
                _config = JsonSerializer.Deserialize<OpenAIAssistantConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                }) ?? new OpenAIAssistantConfig();

                Console.WriteLine($"[Info] OpenAI Assistant 配置載入成功: {configPath}");
                return _config;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] 載入 OpenAI Assistant 配置失敗: {ex.Message}");
                return new OpenAIAssistantConfig();
            }
        }

        /// <summary>
        /// 重新載入配置
        /// </summary>
        public static void ReloadConfig()
        {
            _config = null;
            LoadConfig();
        }

        /// <summary>
        /// 獲取配置文件路徑
        /// </summary>
        private static string GetConfigPath()
        {
            // 先嘗試程式目錄
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
            if (File.Exists(path))
                return path;

            // 再嘗試當前目錄
            path = Path.Combine(Directory.GetCurrentDirectory(), ConfigFileName);
            if (File.Exists(path))
                return path;

            // 最後嘗試專案目錄
            var projectDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            for (int i = 0; i < 5 && projectDir != null; i++)
            {
                var testPath = Path.Combine(projectDir, ConfigFileName);
                if (File.Exists(testPath))
                    return testPath;
                projectDir = Directory.GetParent(projectDir)?.FullName;
            }

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
        }

        /// <summary>
        /// 格式化訊息（支援參數替換）
        /// </summary>
        public static string FormatMessage(string template, params object[] args)
        {
            try
            {
                return args.Length > 0 ? string.Format(template, args) : template;
            }
            catch
            {
                return template;
            }
        }

        /// <summary>
        /// 建構完整的功能說明文字
        /// </summary>
        public static string BuildCapabilitiesText(OpenAIAssistantCapabilities capabilities)
        {
            var result = capabilities.Title + "\n\n";
            
            result += capabilities.CoreAbilities + "\n";
            foreach (var ability in capabilities.CoreAbilitiesList)
            {
                result += ability + "\n";
            }
            result += "\n";

            result += capabilities.IntegratedServices + "\n";
            foreach (var service in capabilities.IntegratedServicesList)
            {
                result += service + "\n";
            }
            result += "\n";

            result += capabilities.Usage + "\n";
            foreach (var usage in capabilities.UsageList)
            {
                result += usage + "\n";
            }
            result += "\n";

            result += capabilities.TechnicalFeatures + "\n";
            foreach (var feature in capabilities.TechnicalFeaturesList)
            {
                result += feature + "\n";
            }

            return result;
        }
    }
}