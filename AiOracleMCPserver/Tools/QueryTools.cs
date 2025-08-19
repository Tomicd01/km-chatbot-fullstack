using Microsoft.EntityFrameworkCore;
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
    [Description("Query tools for executing SQL queries on the Oracle database.")]
    internal class QueryTools
    {
        private readonly AppDbContext _context;
        public QueryTools(AppDbContext context)
        {
            _context = context;
        }
        /*[McpServerTool(Name = "ExecuteQuery")] 
        [Description("Executes an SQL SELECT query on any table in the ORACLE database using ORACLE SQL syntax. " +
                         "Before writing a query, ensure you are querying the right table by Getting database schema. " +  
                         "If column or table names contain lowercase letters or special characters, wrap them in double quotes, e.g. SELECT \"SignalValue\" FROM SIGNALS." +
                         "Before mentioning table name in query, add bmi_cims. e.g. Select * from bmi_cims.Table1")]
        public async Task<string> ExecuteQueryAsync(string query)
        {
            if (!query.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                return "Only SELECT statements are allowed.";
            }

            try
            {
                using var command = _context.Database.GetDbConnection().CreateCommand();
                command.CommandText = query;
                await _context.Database.OpenConnectionAsync();

                using var reader = await command.ExecuteReaderAsync();
                var results = new List<Dictionary<string, object>>();

                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.GetValue(i);
                    }
                    results.Add(row);
                }

                return JsonConvert.SerializeObject(results, Formatting.Indented);
            }
            catch (Exception ex)
            {
                return $"Error executing query: {ex.Message}";
            }
        }*/
    }
}
