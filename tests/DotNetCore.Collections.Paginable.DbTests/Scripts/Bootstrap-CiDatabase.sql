-- CI bootstrap: recreates the database used by DotNetCore.Collections.Paginable.DbTests.
-- Idempotent: safe to re-run against an existing instance.
-- Creates the PaginableDbTests database and the dbo.Int32Samples table, then seeds
-- a deterministic 210-row dataset with Id values 1..210. Every DbTests assertion
-- (TotalMemberCount = 210, page[0].Id = 1, ...) depends on this exact dataset.
--
-- NOTE: dbo.Int32Samples.Id is an IDENTITY column, so inserting explicit Id values
-- REQUIRES SET IDENTITY_INSERT ON. Without it SQL Server raises
--   Msg 544: Cannot insert explicit value for identity column 'Int32Samples'
--            when IDENTITY_INSERT is set to OFF.
-- sqlcmd without -b keeps executing after the error, so the table is silently left
-- EMPTY and the failure only surfaces later as 210-row assertions failing against a
-- 0-row table. Keep the IDENTITY_INSERT pair below intact, and always run this
-- script with `sqlcmd -b` so a broken seed fails the step immediately.

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

SET NOCOUNT ON;

-- Guard against a previously interrupted run having left IDENTITY_INSERT enabled.
SET IDENTITY_INSERT dbo.Int32Samples OFF;

DELETE FROM dbo.Int32Samples;
DBCC CHECKIDENT ('dbo.Int32Samples', RESEED, 0);
GO

SET IDENTITY_INSERT dbo.Int32Samples ON;

;WITH Numbers AS (
    SELECT 1 AS v
    UNION ALL
    SELECT v + 1 FROM Numbers WHERE v < 210
)
INSERT INTO dbo.Int32Samples (Id)
SELECT v FROM Numbers
OPTION (MAXRECURSION 0);

SET IDENTITY_INSERT dbo.Int32Samples OFF;
GO
