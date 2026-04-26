// ╔══════════════════════════════════════════════════════════════════╗
// ║  IFormulaCalculator.cs                                            ║
// ║  חוזה אחיד לכל שיטות החישוב — SOLID principle                   ║
// ╚══════════════════════════════════════════════════════════════════╝
using DynamicFormula.Core.Models;

namespace DynamicFormula.Core.Interfaces
{
    /// <summary>
    /// ממשק אחיד לכל שיטת חישוב נוסחאות דינמי.
    /// כל שיטה (SQL / C# / אחרות) מממשת את הממשק הזה.
    /// </summary>
    public interface IFormulaCalculator
    {
        /// <summary>שם השיטה (לצורך שמירה ב-DB): SQL / CSHARP / TYPESCRIPT.</summary>
        string MethodName { get; }

        /// <summary>
        /// מריץ את כל הנוסחאות על כל רשומות ה-t_data,
        /// שומר תוצאות ב-t_results וזמני ריצה ב-t_log.
        /// </summary>
        Task<IReadOnlyList<PerformanceLog>> RunAllAsync(CancellationToken ct = default);
    }
}
