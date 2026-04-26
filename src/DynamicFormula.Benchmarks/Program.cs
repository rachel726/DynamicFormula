using System.Diagnostics;
using DynamicFormula.Benchmarks;
using DynamicFormula.Calculators;
using DynamicFormula.Core.Interfaces;
using DynamicFormula.Core.Models;
using DynamicFormula.Data;

const string CONNECTION_STRING =
    @"Server=DESKTOP-H70Q0IN\SQLEXPRESS;Database=DynamicFormula;" +
    @"Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=30;Command Timeout=3600;";

string assetsDir = Path.Combine(
    Directory.GetCurrentDirectory(),
    "..", "..", "dashboard", "src", "assets", "data");

Console.OutputEncoding = System.Text.Encoding.UTF8;

var repo = new FormulaRepository(CONNECTION_STRING);

if (args.Contains("--export-only"))
{
    Console.WriteLine();
    Console.WriteLine("▶ Export-only mode: writing report.json with all available results...");
    await JsonExporter.ExportAllAsync(repo, assetsDir);
    return;
}

Console.WriteLine();
Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║       ⚡ Dynamic Formula Engine — Benchmark Runner ⚡         ║");
Console.WriteLine("║       Comparing: SQL  vs  C# (Compiled Lambda)               ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
Console.WriteLine();

var calculators = new IFormulaCalculator[]
{
    new SqlCalculator(repo),
    new CSharpCalculator(repo)
};

var totalSw = Stopwatch.StartNew();
var allLogs = new List<PerformanceLog>();

foreach (var calc in calculators)
{
    try
    {
        var logs = await calc.RunAllAsync();
        allLogs.AddRange(logs);
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  ✗ {calc.MethodName} failed: {ex.Message}");
    }
}

totalSw.Stop();

Console.WriteLine();
Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║                   ★ BENCHMARK SUMMARY ★                     ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");

if (allLogs.Count > 0)
{
    var ranked = allLogs
        .GroupBy(l => l.Method)
        .Select(g => new { Method = g.Key, Avg = g.Average(x => x.RunTimeSec) })
        .OrderBy(x => x.Avg)
        .ToList();

    for (int i = 0; i < ranked.Count; i++)
        Console.WriteLine($"  {(i == 0 ? "🏆" : $"#{i + 1}")} {ranked[i].Method,-12} avg: {ranked[i].Avg:F4}s");
}

Console.WriteLine($"\n   Total: {totalSw.Elapsed.TotalSeconds:F2}s");

