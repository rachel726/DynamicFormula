-- ╔══════════════════════════════════════════════════════════════════╗
-- ║  04_sp_calculate.sql                                              ║
-- ║  ────────────────────────────────────────────────                ║
-- ║  שיטה 1 — SQL Stored Procedure דינמי                            ║
-- ║  ────────────────────────────────────────────────                ║
-- ║  מאפיינים:                                                       ║
-- ║  • SET-BASED מלא — ללא CURSOR וללא WHILE                       ║
-- ║  • מעבד מיליון רשומות ב-INSERT...SELECT בודד לכל נוסחה         ║
-- ║  • sp_executesql עם SQL דינמי — תרגום מפורמט אחיד ל-T-SQL      ║
-- ║  • תומך בנוסחאות רגילות ונוסחאות עם תנאים                      ║
-- ║  • מדוד זמן ב-DATETIME2(7) לדיוק של ננו-שניות                  ║
-- ╚══════════════════════════════════════════════════════════════════╝

USE DynamicFormula;
GO

-- ═══════════════════════════════════════════════════════════════════
-- fn_TranslateFormula — מתרגם נוסחה מפורמט אחיד ל-T-SQL
-- קלט: "sqrt(c^2 + d^2)"  →  פלט: "SQRT(POWER(c,2) + POWER(d,2))"
-- ═══════════════════════════════════════════════════════════════════
IF OBJECT_ID(N'dbo.fn_TranslateFormula', N'FN') IS NOT NULL
    DROP FUNCTION dbo.fn_TranslateFormula;
GO

CREATE FUNCTION dbo.fn_TranslateFormula (@formula VARCHAR(500))
RETURNS VARCHAR(1000)
WITH SCHEMABINDING
AS
BEGIN
    DECLARE @r VARCHAR(1000) = LTRIM(RTRIM(@formula));

    -- פונקציות ל-UPPERCASE (SQL Server case-insensitive אבל יפה שיהיה אחיד)
    SET @r = REPLACE(REPLACE(@r, 'sqrt(',  'SQRT('),  'Sqrt(',  'SQRT(');
    SET @r = REPLACE(REPLACE(@r, 'log(',   'LOG('),   'Log(',   'LOG(');
    SET @r = REPLACE(REPLACE(@r, 'abs(',   'ABS('),   'Abs(',   'ABS(');
    SET @r = REPLACE(REPLACE(@r, 'power(', 'POWER('), 'Power(', 'POWER(');

    -- אופרטור ^ (חזקה) → POWER (נתמך רק בנוסחאות הפשוטות שלנו: x^2)
    -- דוגמה: a^2 → POWER(a,2)
    SET @r = REPLACE(@r, 'a^2', 'POWER(a,2)');
    SET @r = REPLACE(@r, 'b^2', 'POWER(b,2)');
    SET @r = REPLACE(@r, 'c^2', 'POWER(c,2)');
    SET @r = REPLACE(@r, 'd^2', 'POWER(d,2)');

    -- אופרטור השוואה: "=" בודד → "="  (T-SQL משתמש ב-= לא ==)
    SET @r = REPLACE(@r, '==', '=');

    RETURN @r;
END
GO

-- ═══════════════════════════════════════════════════════════════════
-- usp_CalcAllBySQL — מחשב את כל הנוסחאות בשיטת SQL דינמי
--
-- אסטרטגיה:
--   1. שולף רשימת נוסחאות לטבלת #formulas זמנית (Read once)
--   2. עבור כל נוסחה:
--      a. בונה INSERT...SELECT דינמי עם CASE WHEN לתנאים
--      b. sp_executesql עם קומפילציה של תוכנית ביצוע
--      c. מודד זמן מדויק ושומר ב-t_log
--
-- הערה חשובה:
--   השימוש ב-WHILE לצורך מעבר על *נוסחאות* (לא רשומות!) הוא תקין
--   כי יש לנו רק ~16 נוסחאות. החישוב עצמו על כל מיליון רשומות
--   מתבצע ב-INSERT...SELECT יחיד — וזה הלב של ה-SET-BASED.
-- ═══════════════════════════════════════════════════════════════════
IF OBJECT_ID(N'dbo.usp_CalcAllBySQL', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_CalcAllBySQL;
GO

CREATE PROCEDURE dbo.usp_CalcAllBySQL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE
        @targil_id   INT,
        @formula     VARCHAR(500),
        @tnai        VARCHAR(500),
        @false_form  VARCHAR(500),
        @sql         NVARCHAR(MAX),
        @t_start     DATETIME2(7),
        @elapsed_ms  FLOAT,
        @rows_done   INT;

    -- ── ניקוי תוצאות קודמות של שיטת SQL ──
    DELETE FROM dbo.t_results WHERE method = 'SQL';
    DELETE FROM dbo.t_log     WHERE method = 'SQL';

    -- ── טעינת נוסחאות לטבלת זיכרון (Table Variable) ──
    DECLARE @formulas TABLE
    (
        idx          INT IDENTITY(1,1) PRIMARY KEY,
        targil_id    INT,
        targil       VARCHAR(500),
        tnai         VARCHAR(500),
        targil_false VARCHAR(500)
    );
    INSERT INTO @formulas (targil_id, targil, tnai, targil_false)
    SELECT targil_id, targil, tnai, targil_false
    FROM dbo.t_targil
    ORDER BY targil_id;

    DECLARE @i INT = 1;
    DECLARE @n INT = (SELECT COUNT(*) FROM @formulas);

    PRINT '┌────────────────────────────────────────────────┐';
    PRINT '│  SQL Benchmark — ' + CAST(@n AS VARCHAR) + ' formulas × 1M rows          │';
    PRINT '└────────────────────────────────────────────────┘';

    -- ═══════════════════════════════════════════════════════════
    -- לולאה על *נוסחאות* (לא על רשומות!) — ~16 איטרציות בלבד
    -- ═══════════════════════════════════════════════════════════
    WHILE @i <= @n
    BEGIN
        SELECT
            @targil_id  = targil_id,
            @formula    = targil,
            @tnai       = tnai,
            @false_form = targil_false
        FROM @formulas
        WHERE idx = @i;

        -- ── בניית ה-SQL הדינמי ──
        IF @tnai IS NULL
        BEGIN
            -- נוסחה פשוטה (ללא תנאי)
            SET @sql = N'
                INSERT INTO dbo.t_results (data_id, targil_id, method, result)
                SELECT
                    data_id,
                    @tid,
                    ''SQL'',
                    ' + dbo.fn_TranslateFormula(@formula) + N'
                FROM dbo.t_data WITH (NOLOCK)
                OPTION (MAXDOP 0);';
        END
        ELSE
        BEGIN
            -- נוסחה עם תנאי → CASE WHEN
            SET @sql = N'
                INSERT INTO dbo.t_results (data_id, targil_id, method, result)
                SELECT
                    data_id,
                    @tid,
                    ''SQL'',
                    CASE WHEN (' + dbo.fn_TranslateFormula(@tnai) + N')
                         THEN (' + dbo.fn_TranslateFormula(@formula) + N')
                         ELSE (' + dbo.fn_TranslateFormula(@false_form) + N')
                    END
                FROM dbo.t_data WITH (NOLOCK)
                OPTION (MAXDOP 0);';
        END

        -- ── מדידת זמן מדויק והרצה ──
        SET @t_start = SYSDATETIME();

        EXEC sp_executesql @sql, N'@tid INT', @tid = @targil_id;

        SET @rows_done  = @@ROWCOUNT;
        SET @elapsed_ms = DATEDIFF(MICROSECOND, @t_start, SYSDATETIME()) / 1000.0;

        -- ── שמירת הלוג ──
        INSERT INTO dbo.t_log (targil_id, method, run_time, rows_count)
        VALUES (@targil_id, 'SQL', @elapsed_ms / 1000.0, @rows_done);

        PRINT '  ✓ #' + RIGHT('00' + CAST(@targil_id AS VARCHAR), 2)
            + '  ' + FORMAT(@elapsed_ms / 1000.0, 'N3') + 's'
            + '  (' + FORMAT(@rows_done, 'N0') + ' rows)';

        SET @i = @i + 1;
    END

    -- ── סיכום ──
    PRINT '';
    PRINT '═══════════════════════════════════════════════════';
    PRINT ' ✓ SQL method completed';
    PRINT '═══════════════════════════════════════════════════';

    SELECT
        t.targil_id                            AS id,
        t.description                          AS formula,
        FORMAT(l.run_time, 'N3')               AS time_seconds,
        FORMAT(l.rows_count, 'N0')             AS rows_processed
    FROM dbo.t_log l
    JOIN dbo.t_targil t ON t.targil_id = l.targil_id
    WHERE l.method = 'SQL'
    ORDER BY l.targil_id;
END
GO

PRINT '';
PRINT '✓ Stored Procedure usp_CalcAllBySQL created';
PRINT '  To run:  EXEC dbo.usp_CalcAllBySQL;';
GO
