using AiOracleMCPserver.DTOs;
using AiOracleMCPserver.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using ModelContextProtocol.Server;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using System;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace AiOracleMCPserver.Services1
{
    internal class DatabaseService
    {
        private const string ListTablesQuery = @"
            SELECT OWNER, TABLE_NAME 
            FROM ALL_TABLES 
            WHERE OWNER = 'BMI_CIMS' 
            ORDER BY OWNER, TABLE_NAME";
        private readonly AppDbContext _context;
        public DatabaseService(AppDbContext context)
        {
            _context = context;
        }

        //CRUD ON DATABASE

        //CREATE SECTION

        //READ SECTION
        public async Task<DbOperationResult> ListTables()
        {
            var tables = new List<string>();
            var connection = _context.Database.GetDbConnection();

            try
            {
                if (connection.State != System.Data.ConnectionState.Open)
                    await connection.OpenAsync();

                await using var command = connection.CreateCommand();
                command.CommandText = ListTablesQuery;

                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    string owner = reader.GetString(0);
                    string tableName = reader.GetString(1);
                    tables.Add($"{owner}.{tableName}");
                }

                return new DbOperationResult(success: true, data: tables);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ListTables failed: {Message}" , ex.Message);
                return new DbOperationResult(success: false, error: ex.Message);
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
        public async Task<DbOperationResult> DescribeTable(string name)
        {
            string schema = "BMI_CIMS";
            string tableName = name.ToUpper();

            Console.Error.WriteLine($"[DEBUG] DescribeTable called for: {schema}.{tableName}");

            var parameters = new Dictionary<string, object?>
            {
                { "TableName", tableName },
                { "TableSchema", schema }
            };

            const string TableInfoQuery = @"
                SELECT 
                    t.OWNER,
                    t.TABLE_NAME,
                    t.TABLESPACE_NAME,
                    t.STATUS,
                    c.COMMENTS AS DESCRIPTION,
                    o.OBJECT_TYPE
                FROM ALL_TABLES t
                LEFT JOIN ALL_TAB_COMMENTS c ON t.OWNER = c.OWNER AND t.TABLE_NAME = c.TABLE_NAME
                LEFT JOIN ALL_OBJECTS o ON t.OWNER = o.OWNER AND t.TABLE_NAME = o.OBJECT_NAME
                WHERE t.TABLE_NAME = :TableName
                AND t.OWNER = :TableSchema";
            const string ColumnsQuery = @"
                SELECT 
                    col.COLUMN_NAME AS name,
                    col.DATA_TYPE AS type,
                    col.DATA_LENGTH AS length,
                    col.DATA_PRECISION AS precision,
                    col.NULLABLE AS nullable,
                    comm.COMMENTS AS description
                FROM ALL_TAB_COLUMNS col
                LEFT JOIN ALL_COL_COMMENTS comm 
                    ON col.OWNER = comm.OWNER 
                   AND col.TABLE_NAME = comm.TABLE_NAME 
                   AND col.COLUMN_NAME = comm.COLUMN_NAME
                WHERE col.TABLE_NAME = :TableName
                    AND col.OWNER = :TableSchema
                ORDER BY col.COLUMN_ID";

            const string IndexesQuery = @"SELECT 
                    ind.TABLE_OWNER AS owner,
                    ind.TABLE_NAME AS table_name,
                    ind.INDEX_NAME AS name,
                    CASE 
                        WHEN ind.UNIQUENESS = 'UNIQUE' THEN 'UNIQUE'
                        ELSE 'NONUNIQUE'
                    END AS type,
                    LISTAGG(cols.COLUMN_NAME, ', ') WITHIN GROUP (ORDER BY cols.COLUMN_POSITION) AS keys
                FROM ALL_INDEXES ind
                JOIN ALL_IND_COLUMNS cols 
                    ON ind.INDEX_NAME = cols.INDEX_NAME 
                   AND ind.TABLE_OWNER = cols.TABLE_OWNER 
                   AND ind.TABLE_NAME = cols.TABLE_NAME
                WHERE ind.INDEX_TYPE != 'LOB'
                  AND ind.TABLE_OWNER = :TableSchema
                GROUP BY ind.TABLE_OWNER, ind.TABLE_NAME, ind.INDEX_NAME, ind.UNIQUENESS
                ORDER BY ind.TABLE_OWNER, ind.TABLE_NAME, ind.INDEX_NAME";
            const string ConstraintsQuery = @"
                SELECT 
                    ac.OWNER AS owner,
                    ac.TABLE_NAME AS table_name,
                    ac.CONSTRAINT_NAME AS name,
                    ac.CONSTRAINT_TYPE AS type,
                    LISTAGG(acc.COLUMN_NAME, ', ') 
                        WITHIN GROUP (ORDER BY acc.POSITION) AS keys
                FROM ALL_CONSTRAINTS ac
                JOIN ALL_CONS_COLUMNS acc 
                    ON ac.OWNER = acc.OWNER 
                   AND ac.CONSTRAINT_NAME = acc.CONSTRAINT_NAME 
                   AND ac.TABLE_NAME = acc.TABLE_NAME
                WHERE ac.OWNER = :TableSchema
                  AND ac.CONSTRAINT_TYPE IN ('P', 'U') -- P: Primary Key, U: Unique
                GROUP BY ac.OWNER, ac.TABLE_NAME, ac.CONSTRAINT_NAME, ac.CONSTRAINT_TYPE
                ORDER BY ac.OWNER, ac.TABLE_NAME, ac.CONSTRAINT_NAME";
            const string ForeignKeyInformation = @"SELECT  
                    ac.CONSTRAINT_NAME AS name,
                    ac.OWNER AS schema,
                    ac.TABLE_NAME AS table_name,
                    LISTAGG(acc.COLUMN_NAME, ', ') 
                        WITHIN GROUP (ORDER BY acc.POSITION) AS column_names,
                    r_ac.OWNER AS referenced_schema,
                    r_ac.TABLE_NAME AS referenced_table,
                    LISTAGG(r_acc.COLUMN_NAME, ', ') 
                        WITHIN GROUP (ORDER BY r_acc.POSITION) AS referenced_column_names
                FROM ALL_CONSTRAINTS ac
                JOIN ALL_CONS_COLUMNS acc 
                    ON ac.OWNER = acc.OWNER 
                   AND ac.CONSTRAINT_NAME = acc.CONSTRAINT_NAME
                JOIN ALL_CONSTRAINTS r_ac 
                    ON ac.R_OWNER = r_ac.OWNER 
                   AND ac.R_CONSTRAINT_NAME = r_ac.CONSTRAINT_NAME
                JOIN ALL_CONS_COLUMNS r_acc 
                    ON r_ac.OWNER = r_acc.OWNER 
                   AND r_ac.CONSTRAINT_NAME = r_acc.CONSTRAINT_NAME 
                   AND acc.POSITION = r_acc.POSITION
                WHERE ac.CONSTRAINT_TYPE = 'R' -- Foreign Key
                  AND ac.TABLE_NAME = :TableName
                  AND ac.OWNER = :TableSchema
                GROUP BY
                    ac.CONSTRAINT_NAME, ac.OWNER, ac.TABLE_NAME,
                    r_ac.OWNER, r_ac.TABLE_NAME
                ORDER BY ac.CONSTRAINT_NAME";
            var result = new Dictionary<string, object>();
            var connection = _context.Database.GetDbConnection();

            try
            {
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    Console.Error.WriteLine("[DEBUG] Opening DB connection...");
                    await connection.OpenAsync();
                }

                DbCommand PrepareCommand(string sql)
                {
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = sql;

                    if (sql.Contains(":TableName"))
                    {
                        var paramTableName = cmd.CreateParameter();
                        paramTableName.ParameterName = ":TableName";
                        paramTableName.Value = tableName.ToUpperInvariant();
                        cmd.Parameters.Add(paramTableName);
                    }

                    if (sql.Contains(":TableSchema"))
                    {
                        var paramTableSchema = cmd.CreateParameter();
                        paramTableSchema.ParameterName = ":TableSchema";
                        paramTableSchema.Value = schema.ToUpperInvariant();
                        cmd.Parameters.Add(paramTableSchema);
                    }

                    return cmd;
                }

                // 1) Table Info
                try
                {
                    Console.Error.WriteLine("[DEBUG] Executing TableInfoQuery...");
                    Console.Error.WriteLine($"[DEBUG] Parameters: TableName={parameters["TableName"]}, TableSchema={parameters["TableSchema"]}");

                    using (var cmd = PrepareCommand(TableInfoQuery))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        Console.Error.WriteLine("[DEBUG] Reader Executed...");
                        if (await reader.ReadAsync())
                        {
                            Console.Error.WriteLine("[DEBUG] Table info found.");
                            result["table"] = new
                            {
                                owner = reader["OWNER"],
                                table_name = reader["TABLE_NAME"],
                                tablespace = reader["TABLESPACE_NAME"],
                                status = reader["STATUS"],
                                description = reader["DESCRIPTION"] is DBNull ? null : reader["DESCRIPTION"],
                                object_type = reader["OBJECT_TYPE"]
                            };
                        }
                        else
                        {
                            Console.Error.WriteLine($"[DEBUG] Table '{name}' not found in schema '{schema}'.");
                            return new DbOperationResult(success: false, error: $"Table '{name}' not found in schema '{schema}'.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[ERROR] Exception during ExecuteReaderAsync: {ex.Message}");
                    Console.Error.WriteLine($"[STACKTRACE] {ex.StackTrace}");
                    return new DbOperationResult(success: false, error: $"Exception during DB read: {ex.Message}");
                }

                // 2) Columns
                Console.Error.WriteLine("[DEBUG] Executing ColumnsQuery...");
                using (var cmd = PrepareCommand(ColumnsQuery))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var columns = new List<object>();
                    while (await reader.ReadAsync())
                    {
                        columns.Add(new
                        {
                            name = reader["NAME"],
                            type = reader["TYPE"],
                            length = reader["LENGTH"],
                            precision = reader["PRECISION"],
                            nullable = reader["NULLABLE"],
                            description = reader["DESCRIPTION"] is DBNull ? null : reader["DESCRIPTION"]
                        });
                    }
                    Console.Error.WriteLine($"[DEBUG] Columns fetched: {columns.Count}");
                    result["columns"] = columns;
                }

                // 3) Indexes
                Console.Error.WriteLine("[DEBUG] Executing IndexesQuery...");
                try
                {
                    using (var cmd = PrepareCommand(IndexesQuery))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var indexes = new List<object>();
                        while (await reader.ReadAsync())
                        {
                            indexes.Add(new
                            {
                                owner = reader["owner"],       
                                table_name = reader["table_name"],
                                name = reader["name"],
                                type = reader["type"],
                                keys = reader["keys"]
                            });
                        }
                        Console.Error.WriteLine($"[DEBUG] Indexes fetched: {indexes.Count}");
                        result["indexes"] = indexes;
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[ERROR] Exception during IndexesQuery execution: {ex.Message}");
                    Console.Error.WriteLine(ex.StackTrace);
                    return new DbOperationResult(success: false, error: $"Exception during indexes query: {ex.Message}");
                }


                // 4) Constraints
                Console.Error.WriteLine("[DEBUG] Executing ConstraintsQuery...");
                using (var cmd = PrepareCommand(ConstraintsQuery))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var constraints = new List<object>();
                    while (await reader.ReadAsync())
                    {
                        constraints.Add(new
                        {
                            owner = reader["OWNER"],
                            table_name = reader["TABLE_NAME"],
                            name = reader["NAME"],
                            type = reader["TYPE"],
                            keys = reader["KEYS"]
                        });
                    }
                    Console.Error.WriteLine($"[DEBUG] Constraints fetched: {constraints.Count}");
                    result["constraints"] = constraints;
                }

                // 5) Foreign Keys
                try
                {
                    Console.Error.WriteLine("[DEBUG] Executing ForeignKeyInformation...");
                    using (var cmd = PrepareCommand(ForeignKeyInformation))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        var foreignKeys = new List<object>();
                        while (await reader.ReadAsync())
                        {
                            foreignKeys.Add(new
                            {
                                name = reader["NAME"],
                                schema = reader["SCHEMA"],
                                table_name = reader["TABLE_NAME"],
                                column_names = reader["COLUMN_NAMES"],
                                referenced_schema = reader["REFERENCED_SCHEMA"],
                                referenced_table = reader["REFERENCED_TABLE"],
                                referenced_column_names = reader["REFERENCED_COLUMN_NAMES"]
                            });
                        }
                        Console.Error.WriteLine($"[DEBUG] Foreign keys fetched: {foreignKeys.Count}");
                        result["foreignKeys"] = foreignKeys;
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[ERROR] ForeignKeyInformation exception: {ex.Message}");
                    Console.Error.WriteLine(ex.StackTrace);
                    return new DbOperationResult(false, $"ForeignKeyInformation query failed: {ex.Message}");
                }

                Console.Error.WriteLine("[DEBUG] All queries executed successfully.");
                return new DbOperationResult(success: true, data: result);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("DescribeTable failed: {Message}", ex.Message);
                return new DbOperationResult(success: false, error: ex.ToString());
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    Console.Error.WriteLine("[DEBUG] Closing DB connection...");
                    await connection.CloseAsync();
                }
                    
            }

        }
        public async Task<DbOperationResult> ReadData(string sql)
        {
            var conn = _context.Database.GetDbConnection();

            try
            {
                if (conn.State != System.Data.ConnectionState.Open)
                    await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                if (!cmd.CommandText.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                {
                    return new DbOperationResult(success: false, error: "Only SELECT statements are allowed.");
                }
                using var reader = await cmd.ExecuteReaderAsync();

                var results = new List<Dictionary<string, object?>>();

                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object?>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var val = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                        row[reader.GetName(i)] = val;
                    }
                    results.Add(row);
                }

                return new DbOperationResult(success: true, data: results);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("ReadData failed: {Message}", ex.Message);
                return new DbOperationResult(success: false, error: ex.Message);
            }
            finally
            {
                if (conn.State == System.Data.ConnectionState.Open)
                    await conn.CloseAsync();
            }
        }
        public async Task<DateTime> GetCurrentTime()
        {
            return DateTime.Now.ToLocalTime();
        }

        //UPDATE SECTION

        //DELETE SECTION
    }
}
