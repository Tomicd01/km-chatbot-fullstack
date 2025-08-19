using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AiOracleMCPserver.Schema
{
    internal class DatabaseSchema
    {
        [JsonPropertyName("table")]
        public string TableName { get; set; }

        [JsonPropertyName("column")]
        public string ColumnName { get; set; }

        [JsonPropertyName("type")]
        public string? DataType { get; set; }
        [JsonPropertyName("comment")]
        public string? ColumnComment { get; set; }
    }
}
