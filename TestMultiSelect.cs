// 測試多選自動執行功能的簡化版本
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Text.Json;

public class TestMultiSelect
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("=== 測試多選自動執行功能 ===");
        
        // 模擬選項
        var options = new Dictionary<int, string>
        {
            {1, "客戶服務助手"},
            {2, "天氣服務助手"},
            {3, "人力資源助手"},
            {4, "訂單管理助手"}
        };
        
        Console.WriteLine("請選擇代理人（例如: 1,2 表示選擇前兩個）：");
        foreach (var option in options)
        {
            Console.WriteLine($"{option.Key}. {option.Value}");
        }
        
        Console.Write("輸入選項: ");
        var input = Console.ReadLine();
        
        // 解析輸入
        var selectedOptions = ParseSelectedOptions(input ?? "");
        
        if (selectedOptions.Count == 0)
        {
            Console.WriteLine("未選擇任何選項");
            return 1;
        }
        
        Console.WriteLine($"\n您選擇了 {selectedOptions.Count} 個代理人：");
        foreach (var option in selectedOptions)
        {
            Console.WriteLine($"- {options[option]}");
        }
        
        // 判斷是否為首次選擇
        bool isFirstSelection = true; // 模擬首次選擇
        
        Console.WriteLine($"\n開始執行（首次選擇模式：{isFirstSelection}）...");
        
        // 執行選中的代理人
        await ExecuteSelectedAgents(selectedOptions, options, isFirstSelection);
        
        return 0;
    }
    
    public static List<int> ParseSelectedOptions(string input)
    {
        var selectedOptions = new List<int>();
        if (string.IsNullOrWhiteSpace(input))
            return selectedOptions;

        var parts = input.Replace(" ", "").Split(',');
        
        foreach (var part in parts)
        {
            if (int.TryParse(part.Trim(), out int option))
            {
                if (option >= 1 && option <= 4 && !selectedOptions.Contains(option))
                {
                    selectedOptions.Add(option);
                }
            }
        }
        
        return selectedOptions;
    }
    
    public static async Task ExecuteSelectedAgents(List<int> agentOptions, Dictionary<int, string> options, bool isFirstSelection = false)
    {
        int currentIndex = 0;
        int totalAgents = agentOptions.Count;
        
        foreach (var option in agentOptions)
        {
            currentIndex++;
            Console.WriteLine($"\n🚀 正在執行代理人 {option}: {options[option]}... ({currentIndex}/{totalAgents})");
            
            // 模擬代理人執行
            await Task.Delay(1000); // 模擬處理時間
            
            // 模擬某個代理人會 exit
            bool backToMenu = option != 2; // 假設第2個代理人會 exit
            
            if (!backToMenu)
            {
                // 代理人返回 exit，根據是否為首次選擇決定處理方式
                if (currentIndex < totalAgents)
                {
                    Console.WriteLine($"\n⚠️  代理人 {option} 已結束");
                    Console.WriteLine($"還有 {totalAgents - currentIndex} 個代理人待執行：");
                    
                    for (int i = currentIndex; i < agentOptions.Count; i++)
                    {
                        Console.WriteLine($"   - {options[agentOptions[i]]}");
                    }
                    
                    if (isFirstSelection)
                    {
                        // 首次選擇時自動繼續執行
                        Console.WriteLine("🔄 首次多選模式：自動繼續執行剩餘代理人...");
                    }
                    else
                    {
                        // 非首次選擇時詢問使用者
                        Console.Write("是否繼續執行剩餘代理人？(Y/n): ");
                        var continueChoice = Console.ReadLine()?.Trim().ToLower();
                        
                        if (continueChoice == "n" || continueChoice == "no")
                        {
                            Console.WriteLine("📋 已停止執行剩餘代理人，返回");
                            return;
                        }
                        else
                        {
                            Console.WriteLine("✅ 繼續執行剩餘代理人...");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"📋 代理人 {option} 執行完成");
                    return;
                }
            }
            else
            {
                Console.WriteLine($"✅ 代理人 {option} 執行完成，繼續下一個...");
            }
        }
        
        Console.WriteLine($"🎉 所有 {totalAgents} 個代理人都執行完成！");
    }
}