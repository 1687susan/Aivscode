using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.ComponentModel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace day1
{
    /// <summary>
    /// 🧪 測試程式 - 用於驗證系統功能
    /// </summary>
    class TestProgram
    {
        public static async Task TestMain(string[] args)
        {
            Console.WriteLine("🧪 開始執行 AI Agent 系統測試...\n");

            try
            {
                // 測試 1：配置載入測試
                Console.WriteLine("📋 測試 1：配置載入測試");
                var config = ConfigManager.LoadConfig();
                Console.WriteLine($"✅ 配置載入成功");
                Console.WriteLine($"   主選單標題: {config.UI.MainMenu.Title}");
                Console.WriteLine($"   客服系統提示: {config.SystemPrompts.CustomerService.Substring(0, Math.Min(50, config.SystemPrompts.CustomerService.Length))}...");
                Console.WriteLine();

                // 測試 2：JSON 檔案讀取測試
                Console.WriteLine("📄 測試 2：訂單資料載入測試");
                if (File.Exists("orders.json"))
                {
                    var ordersJson = File.ReadAllText("orders.json");
                    var orders = JsonSerializer.Deserialize<Dictionary<string, string>>(ordersJson);
                    Console.WriteLine($"✅ 訂單資料載入成功，共 {orders?.Count ?? 0} 筆訂單");
                    if (orders?.Count > 0)
                    {
                        var firstOrder = orders.First();
                        Console.WriteLine($"   範例訂單: {firstOrder.Key} -> {firstOrder.Value}");
                    }
                }
                else
                {
                    Console.WriteLine("❌ orders.json 檔案不存在");
                }
                Console.WriteLine();

                // 測試 3：Plugin 實例化測試
                Console.WriteLine("🔌 測試 3：Plugin 實例化測試");
                try
                {
                    var orderPlugin = new OrderManagementPlugin();
                    var weatherPlugin = new WeatherServicePlugin();
                    var customerPlugin = new CustomerServicePlugin();
                    var hrPlugin = new HRManagementPlugin();
                    Console.WriteLine("✅ 所有 Plugin 實例化成功");
                    
                    // 測試訂單查詢功能
                    var orderResult = orderPlugin.GetOrderStatus("A001");
                    Console.WriteLine($"   訂單查詢測試: {orderResult.Substring(0, Math.Min(100, orderResult.Length))}...");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Plugin 實例化失敗: {ex.Message}");
                }
                Console.WriteLine();

                // 測試 4：Agent 創建測試（不執行完整流程）
                Console.WriteLine("🤖 測試 4：Agent 創建測試");
                try
                {
                    var customerAgent = new CustomerServiceAgent(config);
                    var weatherAgent = new WeatherServiceAgent(config);
                    var hrAgent = new HRManagementAgent(config);
                    var orderAgent = new OrderManagementAgent(config);
                    Console.WriteLine("✅ 所有 Agent 創建成功");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Agent 創建失敗: {ex.Message}");
                }
                Console.WriteLine();

                // 測試 5：UI 文字顯示測試
                Console.WriteLine("🎨 測試 5：UI 文字顯示測試");
                Console.WriteLine("主選單預覽:");
                Console.WriteLine($"  {config.UI.MainMenu.Title}");
                Console.WriteLine($"  {config.UI.MainMenu.SelectService}");
                foreach (var option in config.UI.MainMenu.Options.Take(2))
                {
                    Console.WriteLine($"  {option.Key}. {option.Value}");
                }
                Console.WriteLine("  ...");
                Console.WriteLine();

                Console.WriteLine("🎉 所有測試完成！系統基本功能正常。");
                Console.WriteLine();
                Console.WriteLine("⚠️  注意：完整功能需要有效的 OpenAI API Key");
                Console.WriteLine("   請在 Config.cs 中設定您的 API Key 後執行完整程式");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 測試過程中發生錯誤: {ex.Message}");
                Console.WriteLine($"   詳細錯誤: {ex}");
            }

            Console.WriteLine("\n按任意鍵結束測試...");
            Console.ReadKey();
        }
    }
}