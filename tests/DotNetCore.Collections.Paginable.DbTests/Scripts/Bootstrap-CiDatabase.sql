-- CI bootstrap: recreates the database used by DotNetCore.Collections.Paginable.DbTests.
-- Idempotent: safe to re-run against an existing instance.
-- The seed script (TestDataScript.sql) relies on an existing Int32Samples table;
-- this script creates the database and the table, then seeds rows 1..210 in identity order.

IF DB_ID(N'PaginableDbTests') IS NULL
    CREATE DATABASE PaginableDbTests;
GO

USE PaginableDbTests;
GO

IF OBJECT_ID(N'dbo.Int32Samples', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Int32Samples
    (
        Id INT IDENTITY(1, 1) NOT NULL,
        CONSTRAINT PK_Int32Samples PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

-- Reset to a deterministic 210-row dataset (1..210).
SET IDENTITY_INSERT dbo.Int32Samples OFF;
DELETE FROM dbo.Int32Samples;
DBCC CHECKIDENT ('dbo.Int32Samples', RESEED, 0);
GO

;WITH Numbers AS (
    SELECT 1 AS v
    UNION ALL
    SELECT v + 1 FROM Numbers WHERE v < 210
)
INSERT INTO dbo.Int32Samples (Id)
SELECT v FROM Numbers
OPTION (MAXRECURSION 0);
GO
