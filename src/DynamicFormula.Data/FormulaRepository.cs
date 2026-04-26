// ╔══════════════════════════════════════════════════════════════════╗
// ║  FormulaRepository.cs                                             ║
// ║  ────────────────────────────────────────────────                ║
// ║  גישה לנתונים — Dapper (קריאות) + SqlBulkCopy (כתיבות)          ║
// ╚══════════════════════════════════════════════════════════════════╝
using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using DynamicFormula.Core.Models;

namespace DynamicFormula.Data
{
    public sealed class FormulaRepository
    {
        private readonly string _connectionString;

        public FormulaRepository(string connectionString)
            => _connectionString = connectionString;

        public async Task<IReadOnlyList<Formula>> GetAllFormulasAsync()
        {
            using var conn = new SqlConnection(_connectionString);
            var rows = await conn.QueryAsync<Formula>(@"
                SELECT targil_id    AS TargilId,
                       targil       AS Targil,
                       tnai         AS Tnai,
                       targil_false AS TargilFalse,
                       description  AS Description
                FROM dbo.t_targil
                ORDER BY targil_id");
            return rows.AsList();
        }

        public async Task<DataRecord[]> LoadAllDataAsync()
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(
                "SELECT data_id, a, b, c, d FROM dbo.t_data WITH (NOLOCK) ORDER BY data_id",
                conn);
            cmd.CommandTimeout = 3600;

            using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

            var list = new List<DataRecord>(1_000_000);
            while (await reader.ReadAsync())
            {
                list.Add(new DataRecord
                {
                    DataId = reader.GetInt32(0),
                    A      = reader.GetDouble(1),
                    B      = reader.GetDouble(2),
                    C      = reader.GetDouble(3),
                    D      = reader.GetDouble(4)
                });
            }
            return list.ToArray();
        }

        public async Task ClearResultsAsync(string method)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.ExecuteAsync(
                "DELETE FROM dbo.t_results WHERE method = @m; DELETE FROM dbo.t_log WHERE method = @m;",
                new { m = method },
                commandTimeout: 120);
        }

        public async Task BulkInsertResultsAsync(IReadOnlyList<CalculationResult> results)
        {
            var dt = new DataTable();
            dt.Columns.Add("data_id",   typeof(int));
            dt.Columns.Add("targil_id", typeof(int));
            dt.Columns.Add("method",    typeof(string));
            dt.Columns.Add("result",    typeof(double));

            foreach (var r in results)
                dt.Rows.Add(r.DataId, r.TargilId, r.Method, r.Result);

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var bulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.TableLock, null)
            {
                DestinationTableName = "dbo.t_results",
                BatchSize            = 5000,
                BulkCopyTimeout      = 3600
            };
            bulk.ColumnMappings.Add("data_id",   "data_id");
            bulk.ColumnMappings.Add("targil_id", "targil_id");
            bulk.ColumnMappings.Add("method",    "method");
            bulk.ColumnMappings.Add("result",    "result");

            await bulk.WriteToServerAsync(dt);
        }

        public async Task SaveLogAsync(PerformanceLog log)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.ExecuteAsync(@"
                INSERT INTO dbo.t_log (targil_id, method, run_time, rows_count)
                VALUES (@TargilId, @Method, @RunTimeSec, @RowsCount);", log);
        }

        public async Task RunSqlStoredProcedureAsync()
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await conn.ExecuteAsync("EXEC dbo.usp_CalcAllBySQL", commandTimeout: 3600);
        }

        // ──────────────────────────────────────────────────────────
        //  Performance Report — משלב את כל 3 השיטות
        // ──────────────────────────────────────────────────────────
        public async Task<IEnumerable<dynamic>> GetPerformanceReportAsync()
        {
            using var conn = new SqlConnection(_connectionString);
            return await conn.QueryAsync(@"
                SELECT
                    t.targil_id,
                    t.description,
                    t.targil,
                    MAX(CASE WHEN l.method = 'SQL'    THEN l.run_time END) AS sql_time,
                    MAX(CASE WHEN l.method = 'CSHARP' THEN l.run_time END) AS csharp_time,
                    MAX(CASE WHEN l.method = 'NODEJS' THEN l.run_time END) AS nodejs_time
                FROM dbo.t_targil t
                LEFT JOIN dbo.t_log l ON t.targil_id = l.targil_id
                GROUP BY t.targil_id, t.description, t.targil
                ORDER BY t.targil_id");
        }

        public async Task<IEnumerable<PerformanceLog>> GetSqlLogsAsync()
        {
            using var conn = new SqlConnection(_connectionString);
            return await conn.QueryAsync<PerformanceLog>(@"
                SELECT targil_id AS TargilId, method AS Method,
                       run_time  AS RunTimeSec, rows_count AS RowsCount
                FROM dbo.t_log WHERE method = 'SQL' ORDER BY targil_id");
        }
    }
}
