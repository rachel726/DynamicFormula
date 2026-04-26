-- ╔══════════════════════════════════════════════════════════════════╗
-- ║  02_seed_data.sql                                                 ║
-- ║  מילוי מיליון רשומות רנדומליות — SET-BASED (מהיר ביותר)         ║
-- ║  זמן ריצה צפוי: 10-30 שניות                                      ║
-- ╚══════════════════════════════════════════════════════════════════╝

USE DynamicFormula;
GO

SET NOCOUNT ON;
DECLARE @t0 DATETIME2(3) = SYSDATETIME();

PRINT '▶ Clearing existing data...';
-- סדר מחיקה לפי FK: קודם child tables, אחר כך parent
DELETE FROM dbo.t_log;
DELETE FROM dbo.t_results;
DELETE FROM dbo.t_data;
DBCC CHECKIDENT ('dbo.t_data',    RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.t_results', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('dbo.t_log',     RESEED, 0) WITH NO_INFOMSGS;
GO

PRINT '▶ Generating 1,000,000 random rows (set-based)...';

-- ──────────────────────────────────────────────────────────────────
-- טכניקה: Tally / Numbers CTE עם CROSS JOIN אקספוננציאלי
-- יוצר 2^20 = 1,048,576 מספרים בזיכרון, אז TOP 1,000,000
-- SET-BASED פעולה אחת — הגישה המהירה ביותר ב-SQL Server
-- ──────────────────────────────────────────────────────────────────
WITH
    E1  AS (SELECT 1 n UNION ALL SELECT 1),       -- 2
    E2  AS (SELECT 1 n FROM E1 a, E1 b),          -- 4
    E4  AS (SELECT 1 n FROM E2 a, E2 b),          -- 16
    E8  AS (SELECT 1 n FROM E4 a, E4 b),          -- 256
    E16 AS (SELECT 1 n FROM E8 a, E8 b),          -- 65,536
    E20 AS (SELECT TOP (1000000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) rn
            FROM E16 a, E16 b)                    -- מיליון מספרים
INSERT INTO dbo.t_data (a, b, c, d)
SELECT
    -- ערכים בטווח 0.1 עד 99.99 (לא 0 כדי למנוע חלוקה באפס וlog(0))
    ROUND(0.1 + (ABS(CHECKSUM(NEWID())) % 99890) / 1000.0, 2),
    ROUND(0.1 + (ABS(CHECKSUM(NEWID())) % 99890) / 1000.0, 2),
    ROUND(0.1 + (ABS(CHECKSUM(NEWID())) % 99890) / 1000.0, 2),
    ROUND(0.1 + (ABS(CHECKSUM(NEWID())) % 99890) / 1000.0, 2)
FROM E20;
GO

-- ── אימות ──────────────────────────────────────────────────────────
DECLARE @cnt INT = (SELECT COUNT(*) FROM dbo.t_data);
DECLARE @elapsed INT = DATEDIFF(MILLISECOND, (SELECT TOP 1 created_at FROM dbo.t_log), SYSDATETIME());
PRINT '';
PRINT '═══════════════════════════════════════════════════';
PRINT ' ✓ Inserted: ' + FORMAT(@cnt, 'N0') + ' rows';
PRINT '═══════════════════════════════════════════════════';
PRINT '';
PRINT '▶ Sample data:';
SELECT TOP 5 data_id, a, b, c, d FROM dbo.t_data ORDER BY data_id;

-- ── סטטיסטיקה לבודק ──────────────────────────────────────────────
SELECT
    COUNT(*)    AS total_rows,
    MIN(a)      AS min_a,    MAX(a) AS max_a,   AVG(a) AS avg_a,
    MIN(b)      AS min_b,    MAX(b) AS max_b,   AVG(b) AS avg_b
FROM dbo.t_data;
GO
