using AiOracleMCPserver.DTOs;
using AiOracleMCPserver.Schema;
using AiOracleMCPserver.Services1;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AiOracleMCPserver.Tools
{
    [McpServerToolType]
    [Description("Tools for understanding and reading ORACLE database.")]
    internal class ReadDatabaseTools
    {
        private readonly DatabaseService _databaseService;
        public ReadDatabaseTools(DatabaseService service)
        {
            _databaseService = service;
        }

        
        [McpServerTool(
            Title = "Get Current Time",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false)]
        [Description("Returns current date and time.")]
        public async Task<DateTime> GetCurrentTimeAsync()
        {
            return await _databaseService.GetCurrentTime();
        }

        [McpServerTool(
            Title = "Read Data",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false)]
        [Description("Executes SQL queries against SQL Database to read data. Before specifying table name in a query, add the schema owner name 'BMI_CIMS'. For example, to read data from table 'BMI_CIMS.EMPLOYEES', use the query 'SELECT * FROM BMI_CIMS.EMPLOYEES'.")]
        public async Task<DbOperationResult> ReadData(
        [Description("SQL query to execute")] string sql)
        {
            return await _databaseService.ReadData(sql);
        }

        [McpServerTool(
            Title = "ListTables",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false)]
        [Description("Lists all tables in the SQL Database.Do not assume name of the table.The schema owner is 'BMI_CIMS'. This must be executed before any other database operations.")]
        public async Task<DbOperationResult> ListTablesAsync()
        {
            return await _databaseService.ListTables();
        }

        [McpServerTool(
            Title = "Describe Table",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false)]
        [Description("Returns table schema. For Additional context about table, run 'TableDescriptionTools.GetTableBusinessContext' tool.")]
        public async Task<DbOperationResult> DescribeTableAsync([Description("Name of table. Do NOT add owner name before table name, just table name.")] string name)
        {
            return await _databaseService.DescribeTable(name);
        }

    }
}
