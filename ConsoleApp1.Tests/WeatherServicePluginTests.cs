using System;
using Xunit;
using day1;

public class WeatherServicePluginTests
{
    [Fact]
    public void QueryWeather_MultiCondition_ReturnsCorrectResults()
    {
        var plugin = new WeatherServicePlugin();
        var request = new WeatherQueryRequest
        {
            Cities = new[] { "Taipei", "Kaohsiung" },
            Dates = new[] { "2025-10-09" },
            WeatherTypes = new[] { "Sunny", "Cloudy" }
        };
        var result = plugin.QueryWeather(request);
        Assert.Contains("Taipei", result);
        Assert.Contains("Kaohsiung", result);
        Assert.DoesNotContain("Taichung", result);
    }

    [Fact]
    public void QueryWeather_NoMatch_ReturnsNotFound()
    {
        var plugin = new WeatherServicePlugin();
        var request = new WeatherQueryRequest
        {
            Cities = new[] { "Tokyo" },
            Dates = new[] { "2025-10-09" },
            WeatherTypes = new[] { "Rainy" }
        };
        var result = plugin.QueryWeather(request);
        Assert.Equal("查無符合條件的天氣資料", result);
    }
}
