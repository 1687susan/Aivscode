using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace day1
{
    // Minimal MCP StdIO connector - launches a process and communicates via stdin/stdout
    public class MCPStdioConnector : IDisposable
    {
        private readonly Process _process;
        private readonly StreamWriter _stdin;
        private readonly StreamReader _stdout;

        public MCPStdioConnector(string exePath, string args = "")
        {
            var psi = new ProcessStartInfo(exePath, args)
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            _process = Process.Start(psi) ?? throw new InvalidOperationException("Unable to start MCP process");
            _stdin = _process.StandardInput;
            _stdout = _process.StandardOutput;
        }

        // Send a JSON request to MCP via stdin
        public async Task SendRequestAsync(object request, CancellationToken ct = default)
        {
            var json = JsonSerializer.Serialize(request);
            await _stdin.WriteLineAsync(json.AsMemory(), ct);
            await _stdin.FlushAsync();
        }

        // Stream responses coming from stdout (line-based)
        public async IAsyncEnumerable<string> StreamResponsesAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            while (!_stdout.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await _stdout.ReadLineAsync();
                if (line is null) yield break;
                yield return line;
            }
        }

        public void Dispose()
        {
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(true);
                }
            }
            catch { }
            _process.Dispose();
        }
    }
}
