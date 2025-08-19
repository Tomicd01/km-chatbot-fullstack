using KMchatbot.Models;
using KMchatbot.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Assistants;
using OpenAI.Chat;
using System.Data.Common;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;
using System.Diagnostics;
using ZLinq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace KMchatbot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly McpService _mcpService;
        private readonly IChatClient _chatClient;
        private readonly MessagesDbContext _messagesDbContext;
        private readonly UserManager<IdentityUser> _userManager;

        public ChatController(McpService mcpService, IChatClient chatClient, MessagesDbContext messagesDbContext, UserManager<IdentityUser> userManager)
        {
            _mcpService = mcpService;
            _chatClient = chatClient;
            _messagesDbContext = messagesDbContext;
            _userManager = userManager;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            var userId = _userManager.GetUserId(User);
            var response = HttpContext.Response;
            var prompt = request.Prompt;

            var messages = Converter.MapStoredToChatMessage(_messagesDbContext.StoredChatMessages
                .Where(m => m.ConversationId == null || (m.ConversationId == request.ConversationId && (m.Role == "user" || m.IsFinalAssistantReply == 1 || m.IsFinalAssistantReply == 2)))
                .OrderByDescending(m => m.Id)
                .ToList(), request.ConversationId);

            var tools  = await _mcpService.ListToolsAsync();

            Microsoft.Extensions.AI.ChatMessage mess = new(ChatRole.User, prompt);
            messages.Add(mess);

            StoredChatMessage storedMess = Converter.MapChatMessageToStored(mess, request.ConversationId);
            storedMess.CreatedAt = DateTime.Now;
            await _messagesDbContext.StoredChatMessages.AddAsync(storedMess);

            while (true)
            {
                var completion = await _chatClient
                    .GetResponseAsync(messages, new()
                    {
                        Tools = [.. tools],
                        Temperature = 0,
                        AllowMultipleToolCalls = true
                    });

                if (completion.FinishReason.Equals(Microsoft.Extensions.AI.ChatFinishReason.ToolCalls) &&
                completion.RawRepresentation is ChatCompletion chatCompletion)
                {
                    mess = new(ChatRole.Assistant, completion.Text);
                    messages.Add(mess);
                    storedMess = Converter.MapChatMessageToStored(mess, request.ConversationId);
                    _messagesDbContext.StoredChatMessages.Add(storedMess);

                    foreach (ChatToolCall toolCall in chatCompletion.ToolCalls)
                    {
                        Microsoft.Extensions.AI.ChatMessage toolResultMessage;
                        switch (toolCall.FunctionName)
                        {
                            //DATABASE QUERYING TOOLS
                            case "list_tables":
                                {
                                    var result = await _mcpService.CallToolAsync("list_tables");
                                    toolResultMessage = result.ToFunctionResultMessage(toolCall.Id, toolCall.FunctionName);
                                    break;
                                }
                            case "describe_table":
                                {
                                    using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                    bool hasTableName = argumentsJson.RootElement.TryGetProperty("name", out JsonElement name);

                                    if (!hasTableName)
                                    {
                                        return BadRequest("The table name argument is required.");
                                    }
                                    var result = await _mcpService.CallToolAsync("describe_table", new Dictionary<string, object> { ["name"] = name });
                                    toolResultMessage = result.ToFunctionResultMessage(toolCall.Id, toolCall.FunctionName);
                                    break;
                                }
                            
                            case "read_data":
                                {
                                    using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                    bool hasQuery = argumentsJson.RootElement.TryGetProperty("sql", out JsonElement sql);

                                    if (!hasQuery)
                                    {
                                        return BadRequest("The query is required.");
                                    }
                                    var result = await _mcpService.CallToolAsync("read_data", new Dictionary<string, object> { ["sql"] = sql });
                                    toolResultMessage = result.ToFunctionResultMessage(toolCall.Id, toolCall.FunctionName);
                                    break;
                                }
                            case "get_table_business_context":
                                {
                                    using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                    bool hasTableName = argumentsJson.RootElement.TryGetProperty("tableName", out JsonElement tableName);

                                    if (!hasTableName)
                                    {
                                        return BadRequest("The table name argument is required.");
                                    }
                                    var result = await _mcpService.CallToolAsync("get_table_business_context", new Dictionary<string, object> { ["tableName"] = tableName });
                                    toolResultMessage = result.ToFunctionResultMessage(toolCall.Id, toolCall.FunctionName);
                                    break;
                                }
                            case "get_current_time":
                                {
                                    var result = await _mcpService.CallToolAsync("get_current_time");
                                    toolResultMessage = result.ToFunctionResultMessage(toolCall.Id, toolCall.FunctionName);
                                    break;
                                }

                            //API TOOLS
                            /*case "get_all_logs":
                                {
                                    var result = await _mcpService.CallToolAsync("get_all_logs");
                                    toolResultMessage = result.ToFunctionResultMessage(toolCall.Id, toolCall.FunctionName);
                                    break;
                                }
                            case "get_log":
                                {
                                    using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                    bool hasStartTime = argumentsJson.RootElement.TryGetProperty("startTime", out JsonElement startTime);
                                    bool hasEndTime = argumentsJson.RootElement.TryGetProperty("endTime", out JsonElement endTime);
                                    bool hasRiCode = argumentsJson.RootElement.TryGetProperty("riCode", out JsonElement riCode);
                                    bool hasNumValues = argumentsJson.RootElement.TryGetProperty("numValues", out JsonElement numValues);
                                    bool hasPeriodType = argumentsJson.RootElement.TryGetProperty("periodType", out JsonElement periodType);
                                    bool hasRangeMode = argumentsJson.RootElement.TryGetProperty("rangeMode", out JsonElement rangeMode);

                                    if (!hasStartTime)
                                    {
                                        return BadRequest("The table name argument is required.");
                                    }
                                    var result = await _mcpService.CallToolAsync("get_log", new Dictionary<string, object>
                                    {
                                        ["startTime"] = startTime,
                                        ["endTime"] = endTime,
                                        ["riCode"] = riCode,
                                        ["numValues"] = numValues,
                                        ["periodType"] = periodType,
                                        ["rangeMode"] = rangeMode,
                                    });
                                    toolResultMessage = result.ToFunctionResultMessage(toolCall.Id, toolCall.FunctionName);
                                    break;
                                }
                            case "get_period_types":
                                {
                                    var result = await _mcpService.CallToolAsync("get_period_types");
                                    toolResultMessage = result.ToFunctionResultMessage(toolCall.Id, toolCall.FunctionName);
                                    break;
                                }
                            case "get_all_tables":
                                {
                                    var result = await _mcpService.CallToolAsync("get_all_tables");
                                    toolResultMessage = result.ToFunctionResultMessage(toolCall.Id, toolCall.FunctionName);
                                    break;
                                }
                            case "get_table":
                                {
                                    using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                    bool hasTableName = argumentsJson.RootElement.TryGetProperty("tableName", out JsonElement tableName);
                                    bool hasPeriodType = argumentsJson.RootElement.TryGetProperty("periodType", out JsonElement periodType);

                                    if (!hasTableName)
                                    {
                                        return BadRequest("The table name argument is required.");
                                    }
                                    if (!hasPeriodType)
                                    {
                                        return BadRequest("The period type argument is required.");
                                    }
                                    var result = await _mcpService.CallToolAsync("get_table", new Dictionary<string, object>
                                    {
                                        ["tableName"] = tableName,
                                        ["periodType"] = periodType,
                                    });
                                    toolResultMessage = result.ToFunctionResultMessage(toolCall.Id, toolCall.FunctionName);
                                    break;
                                }*/
                            

                            default:
                                {
                                    return BadRequest($"Unknown tool call: {toolCall.FunctionName}");
                                }

                        }
                        if (string.IsNullOrEmpty(toolResultMessage.Text))
                        {
                            toolResultMessage = new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, "Sorry for the problem, I will check again.");
                        }

                        messages.Add(toolResultMessage);

                        var storedToolMsg = Converter.MapChatMessageToStored(toolResultMessage, request.ConversationId);
                        storedToolMsg.IsFinalAssistantReply = 2;
                        _messagesDbContext.StoredChatMessages.Add(storedToolMsg);
                        
                    }
                    await _messagesDbContext.SaveChangesAsync();
                    continue;

                }

                else if (completion.FinishReason == Microsoft.Extensions.AI.ChatFinishReason.Stop)
                {
                    mess = new(ChatRole.Assistant, completion.Text);
                    messages.Add(mess);
                    storedMess = Converter.MapChatMessageToStored(mess, request.ConversationId);
                    storedMess.IsFinalAssistantReply = 1;
                    storedMess.CreatedAt = DateTime.Now;
                    _messagesDbContext.StoredChatMessages.Add(storedMess);

                    await _messagesDbContext.SaveChangesAsync();

                    var text = completion.Text ?? "";
                    int chunkSize = 5;
                    for (int i = 0; i < text.Length; i += chunkSize)
                    {
                        var chunk = text.Substring(i, Math.Min(chunkSize, text.Length - i));
                        var bytes = Encoding.UTF8.GetBytes(chunk + "<|>");
                        await response.BodyWriter.WriteAsync(bytes);
                        await response.BodyWriter.FlushAsync();

                        // Optional: add a tiny delay to simulate streaming
                        await Task.Delay(10);
                    }
                    break;
                }

                else
                {
                    return BadRequest("Unknown finish reason.");
                }
            }

            return Ok();
        }

        [Authorize]
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var userId = _userManager.GetUserId(User);
            var conversations = _messagesDbContext.Conversations
                .Where(c => c.UserId == userId)
                .Select(c => new
                {
                    id = c.ConversationId,
                    title = c.Title,
                    messages = c.Messages
                    .OrderBy(m => m.CreatedAt)
                    .Where(m => m.Role == "user" || (m.Role == "assistant" && m.IsFinalAssistantReply == 1))
                    .Select(m => new
                    {
                        role = m.Role,
                        text = m.Text
                    })
                    .ToList()
                }).ToList();

            return Ok(conversations);
        }

        [Authorize]
        [HttpPost("addConversation")]
        public async Task<IActionResult> Add()
        {
            var userId = _userManager.GetUserId(User);
            Conversation conv = new Conversation();
            conv.Title = "Chat " + (_messagesDbContext.Conversations.Where(c=> c.UserId == userId).Count<Conversation>() + 1);
            conv.UserId = userId;
            _messagesDbContext.Add(conv);
            await _messagesDbContext.SaveChangesAsync();
            return Ok(new { id = conv.ConversationId, title = conv.Title, messages = new List<object>()});
        }

    }
}
