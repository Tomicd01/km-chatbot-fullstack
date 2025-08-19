using AiOracleMCPserver.Services1;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace AiOracleMCPserver.Tools
{
    [McpServerToolType]
    [Description("Contains tools for working with logs.")]
    internal class LogTools
    {
        private readonly DatabaseService _dbService;

        public LogTools(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        [McpServerTool(
            Title = "Get Current Time",
            ReadOnly = true,
            Idempotent = true,
            Destructive = false)]
        [Description("Returns current date and time.")]
        public async Task<DateTime> GetCurrentTimeAsync()
        {
            return await _dbService.GetCurrentTime();
        }
        [McpServerTool]
        [Description("Provides all period types.")]
        public async Task<string> GetPeriodTypes()
        {
            HttpClientHandler handler = new HttpClientHandler()
            {
                Credentials = new NetworkCredential("bmi_cims", "Albis123"),
            };
            HttpClient client = new HttpClient(handler);
            var response = await client.GetAsync("http://192.168.200.149/km/items/api/periodTypes");

            return await response.Content.ReadAsStringAsync();
        }

        [McpServerTool]
        [Description("Provides a information about a specific log, based on the provided parameters.")]
        public async Task<string> GetLog(
            [Description("Log values start time period.")] DateTime startTime,
            [Description("Log values end time period.")] DateTime? endTime,
            [Description("Unique identifier of a log.")] int riCode,
            [Description("Number of values to return.")] int numValues,
            [Description("Defines type of period that values get returned (Every 10 minutes, every minute etc.). They are stored in PERIODTYPES table. You just need PTYPE column. For example: PRI, 01M, 15M etc. ")] string periodType,
            [Description("Defines the order of the values returned. Example: rangemode = \"Forwards\" will return values in ascending order, while \"Backwards\" will return them in descending order.")]
            string rangeMode)
        {
            var requestData = new
            {
                startTime = startTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                endTime = endTime?.ToString("yyyy-MM-ddTHH:mm:ss"),
                logs = new[]
                {
                    new
                    {
                        riCode = riCode,
                        numValues = numValues,
                        periodType = periodType,
                        rangeMode = rangeMode
                    }
                }
            };
            HttpClientHandler handler = new HttpClientHandler()
            {
                Credentials = new NetworkCredential("bmi_cims", "Albis123"),
            };
            HttpClient client = new HttpClient(handler);
            var response = await client.PostAsJsonAsync("http://192.168.200.149/kmRuntimeData/api/logData", requestData);
            //var response = await client.GetAsync("http://192.168.200.149/km/items/api/query/datasourceinfos");

            return await response.Content.ReadAsStringAsync();
        }
        [McpServerTool]
        [Description("Provides basic information about tables in the database.")]
        public async Task<string> GetAllTables()
        {
            HttpClientHandler handler = new HttpClientHandler()
            {
                Credentials = new NetworkCredential("bmi_cims", "Albis123"),
            };
            HttpClient client = new HttpClient(handler);
            var response = await client.GetAsync("http://192.168.200.149/km/items/api/query/datasourceinfos");

            return await response.Content.ReadAsStringAsync();
        }

        [McpServerTool]
        [Description("Returns a table.")]
        public async Task<string> GetTable(
            string tableName,
            [Description("Defines type of period that values get returned (Every 10 minutes, every minute etc.). They are stored in PERIODTYPES table. You just need PTYPE column. For example: PRI, 01M, 15M etc. ")]string periodType)
        {
            var requestData = new
            {
                entity = tableName,
                maxRecords = 1000,
                startDate = "2025-08-13T07:32:49",
                endDate = (string?)null,
                extraParameters = new
                {
                    periodType = periodType,
                    rootPath = ""
                }
            };
            HttpClientHandler handler = new HttpClientHandler()
            {
                Credentials = new NetworkCredential("bmi_cims", "Albis123"),
            };
            HttpClient client = new HttpClient(handler);
            var response = await client.PostAsJsonAsync("http://192.168.200.149/km/items/api/query", requestData);

            return await response.Content.ReadAsStringAsync();
        }

        [McpServerTool]
        [Description("Provides basic information about all logs.")]
        public async Task<DbOperationResult> GetAllLogs()
        {
            try
            {
                HttpClientHandler handler = new HttpClientHandler()
                {
                    Credentials = new NetworkCredential("bmi_cims", "Albis123"),
                };
                HttpClient client = new HttpClient(handler);
                var response = await client.GetAsync("http://192.168.200.149/km/items/api/log?query=_&pageSize=1000");

                return new DbOperationResult(success: true, data: await response.Content.ReadAsStringAsync());
            }
            catch(Exception ex)
            {
                return new DbOperationResult(success: false, error: ex.Message);
            }
        }

    }    
}
