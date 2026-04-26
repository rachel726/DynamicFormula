-- ╔══════════════════════════════════════════════════════════════════╗
-- ║  01_schema.sql                                                    ║
-- ║  יצירת מסד נתונים + 4 טבלאות לפי דרישות המבדק                  ║
-- ║  אופטימיזציה: אינדקסים מוגדרים מראש לביצועים מיטביים            ║
-- ╚══════════════════════════════════════════════════════════════════╝

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'DynamicFormula')
BEGIN
    CREATE DATABASE DynamicFormula;
    PRINT '✓ Database created: DynamicFormula';
END
GO

USE DynamicFormula;
GO

-- ── Drop in reverse FK order ──────────────────────────────────────
IF OBJECT_ID(N'dbo.t_log',     N'U') IS NOT NULL DROP TABLE dbo.t_log;
IF OBJECT_ID(N'dbo.t_results', N'U') IS NOT NULL DROP TABLE dbo.t_results;
IF OBJECT_ID(N'dbo.t_targil',  N'U') IS NOT NULL DROP TABLE dbo.t_targil;
IF OBJECT_ID(N'dbo.t_data',    N'U') IS NOT NULL DROP TABLE dbo.t_data;
GO

-- ╔══════════════════════════════════════════════════════════╗
-- ║  t_data — טבלת נתונים (מיליון רשומות)                   ║
-- ╚══════════════════════════════════════════════════════════╝
CREATE TABLE dbo.t_data
(
    data_id INT    IDENTITY(1,1) NOT NULL,
    a       FLOAT  NOT NULL,
    b       FLOAT  NOT NULL,
    c       FLOAT  NOT NULL,
    d       FLOAT  NOT NULL,
    CONSTRAINT PK_t_data PRIMARY KEY CLUSTERED (data_id)
        WITH (FILLFACTOR = 100)   -- מקסימום צפיפות לקריאה מהירה
);
GO

-- ╔══════════════════════════════════════════════════════════╗
-- ║  t_targil — טבלת נוסחאות                                ║
-- ╚══════════════════════════════════════════════════════════╝
CREATE TABLE dbo.t_targil
(
    targil_id    INT           IDENTITY(1,1) NOT NULL,
    targil       VARCHAR(500)  NOT NULL,  -- הנוסחה (או נוסחת TRUE אם יש תנאי)
    tnai         VARCHAR(500)  NULL,      -- תנאי (אופציונלי)
    targil_false VARCHAR(500)  NULL,      -- נוסחת FALSE (אם יש תנאי)
    description  NVARCHAR(200) NULL,      -- תיאור ידידותי
    CONSTRAINT PK_t_targil PRIMARY KEY CLUSTERED (targil_id)
);
GO

-- ╔══════════════════════════════════════════════════════════╗
-- ║  t_results — טבלת תוצאות                                ║
-- ╚══════════════════════════════════════════════════════════╝
CREATE TABLE dbo.t_results
(
    results_id  BIGINT        IDENTITY(1,1) NOT NULL,
    data_id     INT           NOT NULL,
    targil_id   INT           NOT NULL,
    method      VARCHAR(50)   NOT NULL,  -- 'SQL' / 'CSHARP' / 'TYPESCRIPT'
    result      FLOAT         NULL,
    CONSTRAINT PK_t_results  PRIMARY KEY CLUSTERED (results_id),
    CONSTRAINT FK_results_data   FOREIGN KEY (data_id)   REFERENCES dbo.t_data(data_id),
    CONSTRAINT FK_results_targil FOREIGN KEY (targil_id) REFERENCES dbo.t_targil(targil_id)
);
GO

-- אינדקס לאימות תוצאות בין שיטות
CREATE NONCLUSTERED INDEX IX_results_verify
    ON dbo.t_results (targil_id, data_id, method)
    INCLUDE (result);
GO

-- אינדקס לניקוי מהיר לפי method (DELETE WHERE method=... fast)
CREATE NONCLUSTERED INDEX IX_results_method
    ON dbo.t_results (method)
    INCLUDE (results_id);
GO

-- ╔══════════════════════════════════════════════════════════╗
-- ║  t_log — טבלת לוג ביצועים                               ║
-- ╚══════════════════════════════════════════════════════════╝
CREATE TABLE dbo.t_log
(
    log_id      INT           IDENTITY(1,1) NOT NULL,
    targil_id   INT           NOT NULL,
    method      VARCHAR(50)   NOT NULL,
    run_time    FLOAT         NOT NULL,   -- שניות
    rows_count  INT           NULL,
    created_at  DATETIME2(3)  NOT NULL CONSTRAINT DF_log_created DEFAULT SYSDATETIME(),
    CONSTRAINT PK_t_log PRIMARY KEY CLUSTERED (log_id),
    CONSTRAINT FK_log_targil FOREIGN KEY (targil_id) REFERENCES dbo.t_targil(targil_id)
);
GO

CREATE NONCLUSTERED INDEX IX_log_method_targil
    ON dbo.t_log (method, targil_id)
    INCLUDE (run_time, rows_count);
GO

PRINT '✓ 4 tables created successfully';
PRINT '  • t_data    — 1M rows will be inserted';
PRINT '  • t_targil  — formulas catalog';
PRINT '  • t_results — calculation results';
PRINT '  • t_log     — performance timings';
GO

-- ╔══════════════════════════════════════════════════════════╗
-- ║  SQL Login for Node.js (uses SQL Auth, not Windows Auth) ║
-- ║  דרישה: Mixed Mode Authentication מופעל על SQL Server   ║
-- ╚══════════════════════════════════════════════════════════╝
USE master;
GO

IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'formula_user')
BEGIN
    CREATE LOGIN formula_user WITH PASSWORD = 'Formula123!';
    PRINT '✓ Login formula_user created';
END
ELSE
    PRINT '✓ Login formula_user already exists';
GO

USE DynamicFormula;
GO

IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'formula_user')
BEGIN
    CREATE USER formula_user FOR LOGIN formula_user;
    PRINT '✓ User formula_user created';
END
GO

ALTER ROLE db_datareader ADD MEMBER formula_user;
ALTER ROLE db_datawriter ADD MEMBER formula_user;
PRINT '✓ Permissions granted to formula_user';
GO
