using System.ComponentModel;
namespace day1
{
    public class Weather
    {
        [Description("城市")]
        public string City { get; set; } = string.Empty;
        [Description("日期")]
        public string Date { get; set; } = string.Empty;
        [Description("天氣類型")]
        public string Type { get; set; } = string.Empty;
    }
}