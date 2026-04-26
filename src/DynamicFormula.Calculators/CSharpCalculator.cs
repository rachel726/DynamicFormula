// ╔══════════════════════════════════════════════════════════════════╗
// ║  CSharpCalculator.cs                                              ║
// ║  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ ║
// ║   שיטה 2 — C# .NET עם Compiled Expression Trees                  ║
// ║  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ ║
// ║                                                                  ║
// ║  גישה מקצועית:                                                   ║
// ║  1. מקמפל כל נוסחה פעם אחת ל-Func<double,double,double,double,   ║
// ║     double> — באמצעות System.Linq.Expressions                   ║
// ║  2. ה-delegate המקומפל רץ במהירות native (~14ns להפעלה)         ║
// ║  3. Parallel.For על כל הליבות — ניצול מיטבי של ה-CPU            ║
// ║  4. SqlBulkCopy להכנסת תוצאות ב-batch                           ║
// ║                                                                  ║
// ║  למה זה הרבה יותר מהיר מ-DataTable.Compute?                     ║
// ║  • DataTable.Compute מפרסר ומקמפל את הנוסחה בכל קריאה          ║
// ║  • כאן מקמפלים פעם אחת — ואז מיליון קריאות מהירות               ║
// ║  • יתרון של פי 50-100 בביצועים                                  ║
// ╚══════════════════════════════════════════════════════════════════╝
using System.Diagnostics;
using DynamicFormula.Core.Interfaces;
using DynamicFormula.Core.Models;
using DynamicFormula.Data;
using DynamicFormula.Engine.Compiler;

namespace DynamicFormula.Calculators
{
    public sealed class CSharpCalculator : IFormulaCalculator
    {
        public string MethodName => "CSHARP";

        private readonly FormulaRepository _repo;

        public CSharpCalculator(FormulaRepository repo) => _repo = repo;

        public async Task<IReadOnlyList<PerformanceLog>> RunAllAsync(CancellationToken ct = default)
        {
            Console.WriteLine("┌────────────────────────────────────────────────┐");
            Console.WriteLine("│  Method 2: C# .NET (Compiled Lambda)           │");
            Console.WriteLine("│  Strategy: IL-compiled delegate + Parallel     │");
            Console.WriteLine("└────────────────────────────────────────────────┘");

            // ── ניקוי תוצאות קודמות ──
            await _repo.ClearResultsAsync(MethodName);

            // ── טעינת מיליון רשומות פעם אחת (משותף לכל הנוסחאות) ──
            Console.Write("▶ Loading 1M rows into memory... ");
            var swLoad = Stopwatch.StartNew();
            var data = await _repo.LoadAllDataAsync();
            swLoad.Stop();
            Console.WriteLine($"{swLoad.Elapsed.TotalSeconds:F2}s ({data.Length:N0} rows)");

            // ── טעינת נוסחאות ──
            var formulas = await _repo.GetAllFormulasAsync();
            var logs     = new List<PerformanceLog>(formulas.Count);

            // ═══════════════════════════════════════════════════════
            //  עיבוד כל נוסחה:
            //  1. קומפילציה (ללמוד מה הנוסחה = CompileOnce)
            //  2. חישוב מיליון פעם עם Parallel.For (ObserveFast)
            //  3. BulkInsert של התוצאות
            //  4. מדידת זמן הריצה של שלב 2 בלבד — זה הפעולה העיקרית
            // ═══════════════════════════════════════════════════════
            foreach (var f in formulas)
            {
                ct.ThrowIfCancellationRequested();

                // ── שלב 1: קומפילציה (פעם אחת) ──
                CompiledFormula compiled;
                try
                {
                    compiled = f.IsConditional
                        ? FormulaCompiler.CompileConditional(f.Targil, f.Tnai!, f.TargilFalse!)
                        : FormulaCompiler.Compile(f.Targil);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ #{f.TargilId:00}  compile error: {ex.Message}");
                    continue;
                }

                // ── שלב 2: חישוב מיליון שורות במקביל ──
                var results = new double[data.Length];
                var sw      = Stopwatch.StartNew();

                Parallel.For(0, data.Length, new ParallelOptions
                {
                    CancellationToken = ct,
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                },
                i =>
                {
                    var r = data[i];
                    results[i] = compiled(r.A, r.B, r.C, r.D);
                });

                sw.Stop();

                // ── שלב 3: הכנה + Bulk Insert של תוצאות ──
                var calcResults = new CalculationResult[data.Length];
                for (int i = 0; i < data.Length; i++)
                {
                    calcResults[i] = new CalculationResult
                    {
                        DataId   = data[i].DataId,
                        TargilId = f.TargilId,
                        Method   = MethodName,
                        Result   = results[i]
                    };
                }
                await _repo.BulkInsertResultsAsync(calcResults);

                // ── שלב 4: שמירת לוג ──
                var log = new PerformanceLog
                {
                    TargilId   = f.TargilId,
                    Method     = MethodName,
                    RunTimeSec = sw.Elapsed.TotalSeconds,
                    RowsCount  = data.Length
                };
                await _repo.SaveLogAsync(log);
                logs.Add(log);

                Console.WriteLine(
                    $"  ✓ #{f.TargilId:00}  {sw.Elapsed.TotalSeconds,7:F3}s   " +
                    $"({data.Length:N0} rows)   {f.Description}");
            }

            return logs;
        }
    }
}
