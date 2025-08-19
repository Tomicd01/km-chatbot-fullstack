using AiOracleMCPserver.Schema;
using ModelContextProtocol.Server;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiOracleMCPserver.Resources
{
    [McpServerResourceType]
    internal class Resources
    {
        [McpServerResource(UriTemplate = "file://database-schema.json", Name = "JSON schema of the database", MimeType = "application/json")]
        [Description("JSON schema definition for the database structure")]
        public static string DirectTextResource()
        {
            var bytes = File.ReadAllBytes("C:\\Users\\Nignite\\source\\repos\\AiOracleMCPserver\\Resources\\ResourceFiles\\DatabaseSchema.json");
            var text = Encoding.UTF8.GetString(bytes);
            return text;
        }
        
    }
}
