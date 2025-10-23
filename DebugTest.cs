using System;

namespace DebugTest
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("=== 基本測試程式 ===");
            Console.WriteLine("程式啟動成功");
            
            while (true)
            {
                Console.WriteLine("\n請選擇：");
                Console.WriteLine("1. 測試選項1");
                Console.WriteLine("2. 測試選項2");
                Console.WriteLine("3. 退出");
                Console.Write("輸入選項: ");
                
                var input = Console.ReadLine();
                var choice = input?.Trim() ?? "";
                
                Console.WriteLine($"您輸入了: '{choice}'");
                
                if (choice == "3")
                {
                    Console.WriteLine("程式結束");
                    return;
                }
                else if (choice == "1" || choice == "2")
                {
                    Console.WriteLine($"執行選項 {choice}");
                }
                else
                {
                    Console.WriteLine("無效選項，請重新輸入");
                }
            }
        }
    }
}