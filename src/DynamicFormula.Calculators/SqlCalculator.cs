// ╔══════════════════════════════════════════════════════════════════╗
// ║  SqlCalculator.cs                                                 ║
// ║  שיטה 1 — SQL Stored Procedure                                   ║
// ║  קורא ל-usp_CalcAllBySQL שכבר עושה את כל העבודה בתוך ה-DB       ║
// ╚══════════════════════════════════════════════════════════════════╝
using DynamicFormula.Core.Interfaces;
using DynamicFormula.Core.Models;
using DynamicFormula.Data;

namespace DynamicFormula.Calculators
{
    public sealed class SqlCalculator : IFormulaCalculator
    {
        public string MethodName => "SQL";

        private readonly FormulaRepository _repo;

        public SqlCalculator(FormulaRepository repo) => _repo = repo;

        public async Task<IReadOnlyList<PerformanceLog>> RunAllAsync(CancellationToken ct = default)
        {
            Console.WriteLine("┌────────────────────────────────────────────────┐");
            Console.WriteLine("│  Method 1: SQL (Stored Procedure)              │");
            Console.WriteLine("│  Strategy: SET-BASED sp_executesql             │");
            Console.WriteLine("└────────────────────────────────────────────────┘");

            // ה-SP עצמו כבר שומר לוגים בתוך הטרנזקציה — נחזיר אותם
            await _repo.RunSqlStoredProcedureAsync();

            var logs = await _repo.GetSqlLogsAsync();
            return logs.ToList();
        }
    }
}
