using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace KMchatbot.Services
{
    public class McpService
    {
        private readonly IMcpClient _mcpClient;

        public McpService(IChatClient samplingClient)
        {
            _mcpClient = McpClientFactory.CreateAsync(
                new StdioClientTransport(new()
                {
                    Command = "dotnet",
                    Arguments = ["run", "--project", "C:"],
                    Name = "ai-oracle-mcpserver"
                }),
                new McpClientOptions
                {
                    Capabilities = new() { Sampling = new() { SamplingHandler = samplingClient.CreateSamplingHandler() } }
                }).Result; // Consider async init
        }

        public async Task<IList<McpClientTool>> ListToolsAsync() 
        {
            return await _mcpClient.ListToolsAsync();
        }

        public async Task<CallToolResult> CallToolAsync(string name, Dictionary<string, object>? args = null) =>
            await _mcpClient.CallToolAsync(name, args);
    }
}
