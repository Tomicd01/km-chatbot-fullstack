using AiOracleMCPserver.DTOs;
using ModelContextProtocol.Server;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiOracleMCPserver.Tools
{
    [McpServerToolType]
    [Description("Tools for managing and describing database tables in the Oracle schema.")]
    internal class TableDescriptionTools
    {
        private readonly AppDbContext _context;
        public TableDescriptionTools(AppDbContext context)
        {
            _context = context;
        }

        private readonly Dictionary<string, TableContext> _tableContexts = new()
        {
            ["REPITEM"] = new TableContext
            {
                TableName = "Report Item",
                Purpose = "stores information about logs ",
                BusinessRules = new[]
                {
                    "STOPLOG column is boolean with values -1 and 0.",
                },
                CommonUseCases = new[]
                {
                    "User Looking for log information"
                },
                DataSources = "Registration system, patient portal, insurance verification",
                UpdateFrequency = "Real-time during registration and visits",
                /*KeyColumns = new Dictionary<string, string>
                {
                    ["RICODE"] = "Unique identifier id, never reused",
                    ["ALIASNAME"] = "Secondary name of the asset.",
                    ["LAST_NAME"] = "Legal last name, used for matching and billing",
                    ["DOB"] = "Date of birth, critical for patient identification and age calculations"
                }*/
            },
            ["LIMITOBJDATA"] = new TableContext
            {
                TableName = "Limit Object Data",
                Purpose = "History of limits defined in LIMITOBJECT table.",
                BusinessRules = new[]
                {
                    "LIMITID column connects to LIMITOBJECT table, which connects to REPITEM table.",
                    "Limit is exceeded if LIMITID is not null.",
                    "Based on LIMITID, connect to REPITEM and return the log that exceeded the limit."
                },
                CommonUseCases = new[]
                {
                    "User looking for historical limit data."
                },
                UpdateFrequency = "Real-time during registration and visits",
                /*KeyColumns = new Dictionary<string, string>
                {
                    ["RICODE"] = "Unique identifier id, never reused",
                    ["ALIASNAME"] = "Secondary name of the asset.",
                    ["LAST_NAME"] = "Legal last name, used for matching and billing",
                    ["DOB"] = "Date of birth, critical for patient identification and age calculations"
                }*/
            }
        };

        [McpServerTool(
            Title = "Get table business context",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false)]
        [Description("Get additional business context, rules, and usage patterns for database tables.")]
        public async Task<string> GetTableBusinessContextAsync(string tableName)
        {
            if (_tableContexts.TryGetValue(tableName.ToUpper(), out var context))
            {
                return JsonConvert.SerializeObject(context, Formatting.Indented);
            }

            return $"No business context available for table: {tableName}";
        }
    }
}
