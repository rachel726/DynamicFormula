// ╔══════════════════════════════════════════════════════════════════╗
// ║  JsonExporter.cs                                                  ║
// ║  ────────────────────────────────────────────────                ║
// ║  ייצוא תוצאות הבנצ'מרק לקבצי JSON עבור דשבורד Angular           ║
// ║                                                                  ║
// ║  מייצר 3 קבצים בתיקייה dashboard/src/assets/data:               ║
// ║  • report.json       — זמני ריצה לכל נוסחה × שיטה               ║
// ║  • formulas.json     — רשימת הנוסחאות                           ║
// ║  • sample-data.json  — דגימה (100K) לחישוב TypeScript בדפדפן   ║
// ╚══════════════════════════════════════════════════════════════════╝
using System.Text.Json;
using DynamicFormula.Core.Models;
using DynamicFormula.Data;

namespace DynamicFormula.Benchmarks
{
    public static class JsonExporter
    {
        private static readonly JsonSerializerOptions _opts = new()
        {
            WriteIndented        = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static async Task ExportAllAsync(FormulaRepository repo, string outputDir)
        {
            Directory.CreateDirectory(outputDir);

            Console.WriteLine();
            Console.WriteLine("▶ Exporting results to JSON for Angular dashboard...");

            // ── report.json — דוח ביצועים ──
            var report = (await repo.GetPerformanceReportAsync()).ToList();
            await WriteJsonAsync(Path.Combine(outputDir, "report.json"), report);
            Console.WriteLine($"  ✓ report.json        ({report.Count} rows)");

            // ── formulas.json — נוסחאות ──
            var formulas = await repo.GetAllFormulasAsync();
            await WriteJsonAsync(Path.Combine(outputDir, "formulas.json"), formulas);
            Console.WriteLine($"  ✓ formulas.json      ({formulas.Count} formulas)");

            // ── sample-data.json — דגימה לחישוב TS בדפדפן ──
            var allData = await repo.LoadAllDataAsync();
            var sample  = allData.Take(100_000).ToArray();
            await WriteJsonAsync(Path.Combine(outputDir, "sample-data.json"), sample);
            Console.WriteLine($"  ✓ sample-data.json   ({sample.Length:N0} rows)");

            Console.WriteLine();
            Console.WriteLine($"  Output directory: {Path.GetFullPath(outputDir)}");
        }

        private static async Task WriteJsonAsync<T>(string path, T data)
        {
            await using var fs = File.Create(path);
            await JsonSerializer.SerializeAsync(fs, data, _opts);
        }
    }
}
