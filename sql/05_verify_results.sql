-- ============================================================
--  05_verify_results.sql
--  Checks 3 rows per formula via exact index seeks.
--  Runs in under 1 second.
-- ============================================================
USE DynamicFormula;
GO
SET NOCOUNT ON;

-- Compare results: row 1, row 500,000, row 1,000,000 per formula
SELECT
    t.targil_id         AS [#],
    t.description       AS [Formula],
    d.data_id           AS [Row],
    ROUND(r1.result,4)  AS [SQL],
    ROUND(r2.result,4)  AS [C#],
    ROUND(r3.result,4)  AS [Node],
    IIF(ABS(r1.result-r2.result)<0.001
    AND ABS(r2.result-r3.result)<0.001, 'PASS','FAIL') AS [Match]
FROM       dbo.t_targil t
CROSS JOIN (VALUES (1),(500000),(1000000)) d(data_id)
JOIN dbo.t_results r1 ON r1.targil_id=t.targil_id AND r1.data_id=d.data_id AND r1.method='SQL'
JOIN dbo.t_results r2 ON r2.targil_id=t.targil_id AND r2.data_id=d.data_id AND r2.method='CSHARP'
JOIN dbo.t_results r3 ON r3.targil_id=t.targil_id AND r3.data_id=d.data_id AND r3.method='NODEJS'
ORDER BY t.targil_id, d.data_id;

-- Performance summary
SELECT
    method                               AS [Method],
    FORMAT(AVG(run_time),'N3')           AS [Avg/Formula (s)],
    FORMAT(SUM(run_time),'N3')           AS [Total (s)],
    RANK() OVER (ORDER BY AVG(run_time)) AS [Rank]
FROM dbo.t_log
GROUP BY method
ORDER BY AVG(run_time);
GO
