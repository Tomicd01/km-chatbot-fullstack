using AiOracleMCPserver.Entities;
using AiOracleMCPserver.Entities.AnalysisDefinitions;
using AiOracleMCPserver.Entities.Logs;
using AiOracleMCPserver.Entities.QCRules;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolkit.Application.Data.Entities;

namespace AiOracleMCPserver
{
    public class AppDbContext : DbContext
    {
        /// <summary>
        ///   Constructor
        /// </summary>
        public AppDbContext()
        {
            //default ctor uses app.config connection named DomainContext
        }

        /// <summary>
        ///   Constructor with options
        /// </summary>
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }


        
    }
}