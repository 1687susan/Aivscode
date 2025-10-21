namespace day1
{
    // 集中資料管理 DataStore.cs
    using System;
    using System.Collections.Generic;

    public static class DataStore
    {
        public static Dictionary<string, object> Orders { get; } = new()
        {
            { "A001", new { orderId = "A001", status = "已出貨", customerName = "王小明", amount = 1500 } },
            { "A002", new { orderId = "A002", status = "處理中", customerName = "李小華", amount = 2300 } },
            { "A003", new { orderId = "A003", status = "已取消", customerName = "張小美", amount = 980 } },
            { "A004", new { orderId = "A004", status = "已完成", customerName = "陳大明", amount = 3200 } }
        };

        public static Dictionary<string, object> Customers { get; } = new()
        {
            { "王小明", new { name = "王小明", phone = "0912345678", email = "wang@email.com", level = "VIP" } },
            { "李小華", new { name = "李小華", phone = "0923456789", email = "lee@email.com", level = "一般會員" } }
        };

        public static Dictionary<string, object> Employees { get; } = new()
        {
            { "張經理", new Employee { Name = "張經理", EmployeeId = "E001", Department = "業務部", DepartmentCode = "SALES", JobLevel = "經理", Supervisor = "總經理", Seniority = 8, IsRemote = false, IsStationed = false, SupervisorApology = false } },
            { "陳工程師", new Employee { Name = "陳工程師", EmployeeId = "E002", Department = "技術部", DepartmentCode = "TECH", JobLevel = "工程師", Supervisor = "技術經理", Seniority = 5, IsRemote = true, IsStationed = false, SupervisorApology = false } },
            { "王小明", new Employee { Name = "王小明", EmployeeId = "E003", Department = "業務部", DepartmentCode = "SALES", JobLevel = "業務專員", Supervisor = "張經理", Seniority = 3, IsRemote = false, IsStationed = false, SupervisorApology = false } },
            { "陳美麗", new Employee { Name = "陳美麗", EmployeeId = "E004", Department = "人力資源部", DepartmentCode = "HR", JobLevel = "人資助理", Supervisor = "人資經理", Seniority = 2, IsRemote = false, IsStationed = false, SupervisorApology = false } },
            { "李大偉", new Employee { Name = "李大偉", EmployeeId = "E005", Department = "資訊部", DepartmentCode = "IT", JobLevel = "工程師", Supervisor = "資訊經理", Seniority = 6, IsRemote = true, IsStationed = false, SupervisorApology = false } },
            { "林小芳", new Employee { Name = "林小芳", EmployeeId = "E006", Department = "設計部", DepartmentCode = "DESIGN", JobLevel = "設計師", Supervisor = "設計總監", Seniority = 4, IsRemote = false, IsStationed = true, SupervisorApology = false } }
        };

        public static Dictionary<string, int> CityTemperatures { get; } = new()
        {
            { "Taipei", 28 },
            { "Kaohsiung", 31 },
            { "Taichung", 27 },
            { "Tainan", 30 },
            { "Hsinchu", 26 }
        };

        public static Dictionary<string, day1.Weather> WeatherData { get; } = new()
        {
            // 中文城市名稱
            { "台北-2025-10-21", new day1.Weather { City = "台北", Date = "2025-10-21", Type = "多雲" } },
            { "高雄-2025-10-21", new day1.Weather { City = "高雄", Date = "2025-10-21", Type = "晴朗" } },
            { "台中-2025-10-21", new day1.Weather { City = "台中", Date = "2025-10-21", Type = "小雨" } },
            { "台南-2025-10-21", new day1.Weather { City = "台南", Date = "2025-10-21", Type = "晴朗" } },
            { "新竹-2025-10-21", new day1.Weather { City = "新竹", Date = "2025-10-21", Type = "陰天" } },
            // 英文城市名稱（相容性）
            { "Taipei-2025-10-21", new day1.Weather { City = "Taipei", Date = "2025-10-21", Type = "Cloudy" } },
            { "Kaohsiung-2025-10-21", new day1.Weather { City = "Kaohsiung", Date = "2025-10-21", Type = "Sunny" } },
            { "Taichung-2025-10-21", new day1.Weather { City = "Taichung", Date = "2025-10-21", Type = "Rainy" } },
            { "Tainan-2025-10-21", new day1.Weather { City = "Tainan", Date = "2025-10-21", Type = "Sunny" } },
            { "Hsinchu-2025-10-21", new day1.Weather { City = "Hsinchu", Date = "2025-10-21", Type = "Overcast" } }
        };
    }
}
