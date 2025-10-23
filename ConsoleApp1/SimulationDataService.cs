using System;
using System.Collections.Generic;
using System.Linq;

namespace day1
{
    /// <summary>
    /// 模擬資料服務 - 提供天氣查詢、員工查詢等模擬回應功能
    /// </summary>
    public static class SimulationDataService
    {
        /// <summary>
        /// 生成智能模擬回應（支援複合查詢）
        /// </summary>
        public static string GenerateSimulatedResponse(string input)
        {
            var lowerInput = input.ToLower();
            var responses = new List<string>();
            
            // 檢查所有可能的查詢類型並組合回應
            
            // 智能員工查詢處理
            if (lowerInput.Contains("員工") || lowerInput.Contains("hr"))
            {
                responses.Add(EmployeeDataService.GenerateEmployeeResponse(lowerInput));
            }
            
            // 智能天氣查詢處理
            if (lowerInput.Contains("天氣") || lowerInput.Contains("weather"))
            {
                responses.Add(WeatherDataService.GenerateWeatherResponse(lowerInput));
            }
            
            // 客戶服務查詢處理
            if (lowerInput.Contains("客戶") || lowerInput.Contains("customer"))
            {
                responses.Add(@"👥 客戶服務回應 (模擬):
✅ 已為您查詢相關資訊
📞 如需進一步協助，請聯繫客服專線
📧 或發送郵件至客服信箱");
            }
            
            // 訂單查詢處理
            if (lowerInput.Contains("訂單") || lowerInput.Contains("order"))
            {
                responses.Add(@"📦 訂單查詢結果 (模擬):
🔸 訂單 #12345 - 處理中
🔸 訂單 #12346 - 已出貨
🔸 訂單 #12347 - 已完成
📊 總計 3 筆訂單");
            }
            
            // 如果有多個回應，組合它們
            if (responses.Count > 0)
            {
                return string.Join("\n\n" + new string('─', 50) + "\n\n", responses);
            }
            
            // 沒有匹配到任何特定查詢類型的預設回應
            return $"💭 已收到您的查詢: \"{input}\"\n🔄 這是模擬回應，實際功能需要設定 OpenAI API 金鑰";
        }
    }

    /// <summary>
    /// 員工資料服務 - 處理員工相關查詢
    /// </summary>
    public static class EmployeeDataService
    {
        /// <summary>
        /// 員工資料庫 (模擬) - 使用簡化字典儲存
        /// </summary>
        private static readonly Dictionary<string, List<(string name, string position, string office)>> EmployeeDatabase = new()
        {
            ["台北"] = new List<(string, string, string)>
            {
                ("張小明", "工程師", "台北辦公室"),
                ("王小美", "專案經理", "台北辦公室"),
                ("陳大華", "設計師", "台北辦公室")
            },
            ["新北"] = new List<(string, string, string)>
            {
                ("李小華", "設計師", "新北辦公室"),
                ("林志明", "業務員", "新北辦公室")
            },
            ["台中"] = new List<(string, string, string)>
            {
                ("黃小芳", "工程師", "台中辦公室"),
                ("周大偉", "行銷專員", "台中辦公室")
            }
        };

        /// <summary>
        /// 生成員工查詢回應
        /// </summary>
        public static string GenerateEmployeeResponse(string lowerInput)
        {
            // 檢查是否詢問特定條件
            if (lowerInput.Contains("台北") && lowerInput.Contains("工程師"))
            {
                var engineers = EmployeeDatabase["台北"].Where(emp => emp.position.Contains("工程師")).ToList();
                var result = "📋 台北辦公室工程師查詢結果 (模擬):\n";
                foreach (var emp in engineers)
                {
                    result += $"🔸 {emp.name} - {emp.position} - {emp.office}\n";
                }
                result += $"📊 台北辦公室共有 {engineers.Count} 位工程師";
                return result;
            }
            else if (lowerInput.Contains("工程師"))
            {
                var allEngineers = new List<(string name, string position, string office)>();
                foreach (var office in EmployeeDatabase.Values)
                {
                    allEngineers.AddRange(office.Where(emp => emp.position.Contains("工程師")));
                }
                var result = "📋 工程師查詢結果 (模擬):\n";
                foreach (var emp in allEngineers)
                {
                    result += $"🔸 {emp.name} - {emp.position} - {emp.office}\n";
                }
                result += $"📊 全公司共有 {allEngineers.Count} 位工程師";
                return result;
            }
            else if (lowerInput.Contains("台北"))
            {
                var employees = EmployeeDatabase["台北"];
                var result = "📋 台北辦公室員工查詢結果 (模擬):\n";
                foreach (var emp in employees)
                {
                    result += $"🔸 {emp.name} - {emp.position} - {emp.office}\n";
                }
                result += $"📊 台北辦公室共有 {employees.Count} 位員工";
                return result;
            }
            else if (lowerInput.Contains("新北"))
            {
                var employees = EmployeeDatabase["新北"];
                var result = "📋 新北辦公室員工查詢結果 (模擬):\n";
                foreach (var emp in employees)
                {
                    result += $"🔸 {emp.name} - {emp.position} - {emp.office}\n";
                }
                result += $"📊 新北辦公室共有 {employees.Count} 位員工";
                return result;
            }
            else if (lowerInput.Contains("台中"))
            {
                var employees = EmployeeDatabase["台中"];
                var result = "📋 台中辦公室員工查詢結果 (模擬):\n";
                foreach (var emp in employees)
                {
                    result += $"🔸 {emp.name} - {emp.position} - {emp.office}\n";
                }
                result += $"📊 台中辦公室共有 {employees.Count} 位員工";
                return result;
            }
            else
            {
                var result = "📋 員工資料查詢結果 (模擬):\n";
                var allEmployees = new List<(string name, string position, string office)>();
                foreach (var office in EmployeeDatabase.Values)
                {
                    allEmployees.AddRange(office);
                }
                foreach (var emp in allEmployees)
                {
                    result += $"🔹 {emp.name} - {emp.position} - {emp.office}\n";
                }
                result += $"📊 總計 {allEmployees.Count} 位員工";
                return result;
            }
        }
    }

    // Employee 類別已在其他檔案中定義

    /// <summary>
    /// 天氣資料服務 - 處理全球天氣查詢
    /// </summary>
    public static class WeatherDataService
    {
        /// <summary>
        /// 全球城市天氣資料庫 (模擬)
        /// </summary>
        private static readonly Dictionary<string, string> GlobalLocationsMap = new()
        {
            // 台灣城市
            ["台北"] = "台北, 台灣", ["taipeh"] = "台北, 台灣", ["taipei"] = "台北, 台灣",
            ["新北"] = "新北, 台灣", ["新竹"] = "新竹, 台灣", ["hsinchu"] = "新竹, 台灣",
            ["台中"] = "台中, 台灣", ["taichung"] = "台中, 台灣",
            ["台南"] = "台南, 台灣", ["tainan"] = "台南, 台灣",
            ["高雄"] = "高雄, 台灣", ["kaohsiung"] = "高雄, 台灣",
            
            // 亞洲主要城市
            ["東京"] = "東京, 日本", ["tokyo"] = "東京, 日本",
            ["首爾"] = "首爾, 韓國", ["seoul"] = "首爾, 韓國",
            ["北京"] = "北京, 中國", ["beijing"] = "北京, 中國",
            ["上海"] = "上海, 中國", ["shanghai"] = "上海, 中國",
            ["香港"] = "香港", ["hong kong"] = "香港",
            ["新加坡"] = "新加坡", ["singapore"] = "新加坡",
            
            // 歐美主要城市
            ["倫敦"] = "倫敦, 英國", ["london"] = "倫敦, 英國",
            ["巴黎"] = "巴黎, 法國", ["paris"] = "巴黎, 法國",
            ["紐約"] = "紐約, 美國", ["new york"] = "紐約, 美國",
            ["洛杉磯"] = "洛杉磯, 美國", ["los angeles"] = "洛杉磯, 美國"
        };

        /// <summary>
        /// 生成天氣查詢回應
        /// </summary>
        public static string GenerateWeatherResponse(string lowerInput)
        {
            var timeNow = DateTime.Now.ToString("HH:mm");
            var dateNow = DateTime.Now.ToString("yyyy-MM-dd");
            
            // 解析用戶輸入的城市
            var detectedLocation = ParseLocationFromInput(lowerInput);
            
            if (!string.IsNullOrEmpty(detectedLocation))
            {
                return GenerateDetailedWeatherResponse(detectedLocation, timeNow, dateNow);
            }
            else
            {
                return GeneratePopularCitiesWeatherOverview(timeNow, dateNow);
            }
        }

        /// <summary>
        /// 從用戶輸入中解析地點資訊
        /// </summary>
        private static string ParseLocationFromInput(string lowerInput)
        {
            foreach (var location in GlobalLocationsMap)
            {
                if (lowerInput.Contains(location.Key))
                {
                    return location.Value;
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// 生成詳細天氣回應
        /// </summary>
        private static string GenerateDetailedWeatherResponse(string location, string timeNow, string dateNow)
        {
            // 模擬從天氣 API 獲取的資料
            var weatherData = GenerateSimulatedWeatherData(location);
            
            return $@"{weatherData.emoji} {location} 天氣資訊：
📍 地點: {location}
🌡️  溫度: {weatherData.temperature}
☁️  天氣: {weatherData.condition}
💧 濕度: {weatherData.humidity}
💨 風速: {weatherData.windSpeed}

💡 今日建議: {weatherData.recommendation}

📅 查詢日期: {dateNow}
🕐 更新時間: {timeNow}

{GenerateGlobalWeatherSourceInfo()}";
        }

        /// <summary>
        /// 生成模擬天氣資料
        /// </summary>
        private static (string temperature, string condition, string humidity, 
                       string windSpeed, string emoji, string recommendation) GenerateSimulatedWeatherData(string location)
        {
            // 根據不同地區生成合理的天氣資料
            var random = new Random(location.GetHashCode() + DateTime.Now.Day);
            
            var temperatures = new[] { "18°C", "22°C", "25°C", "28°C", "15°C", "32°C" };
            var conditions = new[] { "晴朗", "多雲", "小雨", "陰天", "雷陣雨" };
            var emojis = new[] { "☀️", "🌤️", "🌧️", "☁️", "⛈️" };
            var recommendations = new[] { 
                "適合戶外活動", "建議攜帶雨具", "注意防曬", "穿著輕便", 
                "添加衣物保暖", "避免長時間戶外活動" 
            };

            var tempIndex = random.Next(temperatures.Length);
            var conditionIndex = random.Next(conditions.Length);
            
            return (
                temperature: temperatures[tempIndex],
                condition: conditions[conditionIndex],
                humidity: $"{random.Next(40, 85)}%",
                windSpeed: $"{random.Next(5, 25)} km/h",
                emoji: emojis[conditionIndex],
                recommendation: recommendations[random.Next(recommendations.Length)]
            );
        }

        /// <summary>
        /// 生成熱門城市天氣概況
        /// </summary>
        private static string GeneratePopularCitiesWeatherOverview(string timeNow, string dateNow)
        {
            return $@"🌍 全球熱門城市天氣概況 ({dateNow}):

┌─── 亞洲地區 ───┐
📍 台北: 25°C, 多雲 🌤️
📍 東京: 18°C, 晴朗 ☀️
📍 首爾: 15°C, 小雨 🌧️
📍 新加坡: 32°C, 雷陣雨 ⛈️
📍 香港: 28°C, 多雲 ☁️

┌─── 歐洲地區 ───┐
📍 倫敦: 12°C, 陰天 ☁️
📍 巴黎: 16°C, 多雲 🌤️

┌─── 美洲地區 ───┐
📍 紐約: 8°C, 雪 ❄️
📍 洛杉磯: 26°C, 晴朗 ☀️

🕐 更新時間: {timeNow}
💡 輸入特定城市名稱可獲得詳細天氣資訊
🌐 支援全球主要城市查詢

{GenerateGlobalWeatherSourceInfo()}";
        }

        /// <summary>
        /// 產生全球天氣資料來源資訊
        /// </summary>
        public static string GenerateGlobalWeatherSourceInfo()
        {
            return @"
📊 全球天氣資料來源：
🌍 OpenWeatherMap: https://openweathermap.org/
🌐 Weather API: https://www.weatherapi.com/
🗺️  AccuWeather: https://www.accuweather.com/
⚡ WeatherStack: https://weatherstack.com/

🏛️ 台灣地區官方來源：
🇹🇼 中央氣象署: https://www.cwa.gov.tw/
📱 生活氣象: https://www.cwa.gov.tw/V8/C/L/

⚠️  重要提醒：
• 此為演示用模擬資料，實際應用請整合真實天氣 API
• 建議使用多個資料源進行交叉驗證
• 關鍵天氣警報請參考當地官方氣象部門";
        }
    }
}