#pragma warning disable OPENAI001 // OpenAI API is in beta
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OpenAI.Assistants;

namespace day1
{
    public class MCPAssistantTool
    {
        private readonly string _mcpCommand;
        private readonly string _mcpArgs;
        
        public MCPAssistantTool(string mcpCommand, string mcpArgs = "")
        {
            _mcpCommand = mcpCommand ?? throw new ArgumentNullException(nameof(mcpCommand));
            _mcpArgs = mcpArgs ?? "";
        }

        /*
        public FunctionToolDefinition CreateToolDefinition()
        {
            // Temporarily disabled due to API compatibility issues
            // Will be implemented when OpenAI SDK structure is clarified
            throw new NotImplementedException("Tool definitions temporarily disabled");
        }
        */

        public async Task<string> ExecuteToolAsync(string functionArguments, CancellationToken ct = default)
        {
            try
            {
                var args = JsonSerializer.Deserialize<Dictionary<string, object>>(functionArguments);
                var service = args?["service"]?.ToString() ?? "unknown";
                var action = args?["action"]?.ToString() ?? "unknown";
                var query = args?["query"]?.ToString() ?? "";
                var data = args?.ContainsKey("data") == true ? args["data"] : null;

                // 根據服務類型路由到適當的處理邏輯
                return service switch
                {
                    "customer_service" => await HandleCustomerServiceAsync(action, query, data, ct),
                    "weather" => await HandleWeatherServiceAsync(action, query, data, ct),
                    "hr" => await HandleHRServiceAsync(action, query, data, ct),
                    "order" => await HandleOrderServiceAsync(action, query, data, ct),
                    _ => await HandleGenericMCPCallAsync(service, action, query, data, ct)
                };
            }
            catch (Exception ex)
            {
                return $"MCP Tool execution error: {ex.Message}";
            }
        }

        private async Task<string> HandleCustomerServiceAsync(string action, string query, object? data, CancellationToken ct)
        {
            var mcpRequest = new
            {
                service = "customer_service",
                action = action,
                query = query,
                data = data,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };

            return await CallMCPServerAsync(mcpRequest, ct);
        }

        private async Task<string> HandleWeatherServiceAsync(string action, string query, object? data, CancellationToken ct)
        {
            var mcpRequest = new
            {
                service = "weather",
                action = action,
                location = query,
                data = data,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };

            return await CallMCPServerAsync(mcpRequest, ct);
        }

        private async Task<string> HandleHRServiceAsync(string action, string query, object? data, CancellationToken ct)
        {
            var mcpRequest = new
            {
                service = "hr",
                action = action,
                query = query,
                employee_data = data,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };

            return await CallMCPServerAsync(mcpRequest, ct);
        }

        private async Task<string> HandleOrderServiceAsync(string action, string query, object? data, CancellationToken ct)
        {
            var mcpRequest = new
            {
                service = "order",
                action = action,
                query = query,
                order_data = data,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };

            return await CallMCPServerAsync(mcpRequest, ct);
        }

        private async Task<string> HandleGenericMCPCallAsync(string service, string action, string query, object? data, CancellationToken ct)
        {
            var mcpRequest = new
            {
                service = service,
                action = action,
                query = query,
                data = data,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            };

            return await CallMCPServerAsync(mcpRequest, ct);
        }

        private async Task<string> CallMCPServerAsync(object request, CancellationToken ct)
        {
            try
            {
                using var connector = new MCPStdioConnector(_mcpCommand, _mcpArgs);
                
                // Send request to MCP server
                await connector.SendRequestAsync(request);

                // Collect responses with timeout
                var responses = new List<string>();
                var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(15));

                await foreach (var line in connector.StreamResponsesAsync(timeoutCts.Token))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        responses.Add(line.Trim());
                    }
                    
                    // Limit response collection to prevent overflow
                    if (responses.Count >= 10) break;
                }

                if (responses.Count == 0)
                {
                    return "No response from MCP server";
                }

                // Format and return consolidated response
                var result = string.Join("\n", responses);
                return FormatMCPResponse(result);
            }
            catch (OperationCanceledException)
            {
                return "MCP server call timed out";
            }
            catch (Exception ex)
            {
                return $"MCP server error: {ex.Message}";
            }
        }

        private string FormatMCPResponse(string rawResponse)
        {
            // Try to parse as JSON for better formatting
            try
            {
                using var doc = JsonDocument.Parse(rawResponse);
                return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
            }
            catch
            {
                // Return raw response if not valid JSON
                return rawResponse;
            }
        }

        /*
        public static FunctionToolDefinition[] GetAllToolDefinitions()
        {
            // Temporarily disabled due to API compatibility issues
            return new FunctionToolDefinition[0];
        }
        */
    }
}