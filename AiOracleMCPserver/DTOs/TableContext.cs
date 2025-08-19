using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiOracleMCPserver.DTOs
{
    internal class TableContext
    {
        public string? TableName { get; set; }
        public string? Purpose { get; set; }
        public string[]? BusinessRules { get; set; }
        public string[]? CommonUseCases { get; set; }
        public string? DataSources { get; set; }
        public string? UpdateFrequency { get; set; }
        public Dictionary<string, string>? KeyColumns { get; set; }
        public string[]? RelatedProcesses { get; set; }
    }
}
