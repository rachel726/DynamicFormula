// ╔══════════════════════════════════════════════════════════════════╗
// ║  Models.cs                                                        ║
// ║  מודלים של המערכת — immutable, clean, self-documenting          ║
// ╚══════════════════════════════════════════════════════════════════╝
namespace DynamicFormula.Core.Models
{
    /// <summary>
    /// רשומה אחת מטבלת t_data.
    /// </summary>
    public sealed class DataRecord
    {
        public int    DataId { get; init; }
        public double A      { get; init; }
        public double B      { get; init; }
        public double C      { get; init; }
        public double D      { get; init; }
    }

    /// <summary>
    /// נוסחה מטבלת t_targil. תומך בנוסחה פשוטה או נוסחה מותנית.
    /// </summary>
    public sealed class Formula
    {
        public int     TargilId    { get; init; }
        public string  Targil      { get; init; } = string.Empty;
        public string? Tnai        { get; init; }
        public string? TargilFalse { get; init; }
        public string? Description { get; init; }

        /// <summary>האם זו נוסחה עם תנאי.</summary>
        public bool IsConditional => !string.IsNullOrWhiteSpace(Tnai);
    }

    /// <summary>
    /// תוצאת חישוב של נוסחה על רשומה בודדת.
    /// </summary>
    public sealed class CalculationResult
    {
        public int    DataId   { get; init; }
        public int    TargilId { get; init; }
        public string Method   { get; init; } = string.Empty;
        public double Result   { get; init; }
    }

    /// <summary>
    /// לוג ביצועים לנוסחה מסוימת בשיטה מסוימת.
    /// </summary>
    public sealed class PerformanceLog
    {
        public int    TargilId   { get; init; }
        public string Method     { get; init; } = string.Empty;
        public double RunTimeSec { get; init; }
        public int    RowsCount  { get; init; }
    }
}
