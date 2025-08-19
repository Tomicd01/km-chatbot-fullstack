using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiOracleMCPserver.Prompts
{
    [McpServerPromptType]
    internal class EntryPrompt
    {
        [McpServerPrompt, Description("Prompt to load table schemas from a database.")]
        public ChatMessage GetDatabaseSchema() 
            => new(ChatRole.System, "Get database schema of provided Oracle database.");
    }
}
