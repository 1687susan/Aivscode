using System;
using System.Threading;
using System.Threading.Tasks;

namespace day1
{
    public static class MCPStdioTest
    {
        public static async Task RunOnce()
        {
            var script = "mock_mcp.ps1";
            var connector = new MCPStdioConnector("powershell.exe", $"-File {script}");
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            await connector.SendRequestAsync(new { action = "ping", payload = "hello" }, cts.Token);

            await foreach (var line in connector.StreamResponsesAsync(cts.Token))
            {
                Console.WriteLine($"MCP -> {line}");
            }

            connector.Dispose();
        }
    }
}
