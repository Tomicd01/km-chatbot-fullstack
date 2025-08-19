using AiOracleMCPserver;
using AiOracleMCPserver.Prompts;
using AiOracleMCPserver.Services1;
using AiOracleMCPserver.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

var builder = Host.CreateEmptyApplicationBuilder(settings: null);

builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<ReadDatabaseTools>()
    .WithTools<TableDescriptionTools>()
    .WithPromptsFromAssembly()
    .WithResourcesFromAssembly();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseOracle("");
});
builder.Services.AddSingleton<ReadDatabaseTools>();
builder.Services.AddSingleton<QueryTools>();
builder.Services.AddSingleton<TableDescriptionTools>();
builder.Services.AddSingleton<DatabaseService>();

var app = builder.Build();

var scope = app.Services.CreateScope();
var discoveryTools = scope.ServiceProvider.GetRequiredService<ReadDatabaseTools>();
var queryTools = scope.ServiceProvider.GetRequiredService<QueryTools>();
var dbcontext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

async Task<bool> TestConnectionAsync()
{
    try
    {
        return await dbcontext.Database.CanConnectAsync();
    }
    catch (Exception ex)
    {
        // log or handle exception if needed
        Console.WriteLine($"Connection failed: {ex.Message}");
        return false;
    }
}

await TestConnectionAsync();


await app.RunAsync();