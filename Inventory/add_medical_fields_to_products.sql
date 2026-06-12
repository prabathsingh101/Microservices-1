-- Migration Script: Add GenericName, Manufacturer, and ScheduleClass to Products table

-- 1. Add GenericName column (varchar/nvarchar)
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = N'GenericName'
)
BEGIN
    ALTER TABLE [Products] ADD [GenericName] NVARCHAR(250) NULL;
END;

-- 2. Add Manufacturer column (varchar/nvarchar)
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = N'Manufacturer'
)
BEGIN
    ALTER TABLE [Products] ADD [Manufacturer] NVARCHAR(150) NULL;
END;

-- 3. Add ScheduleClass column (varchar/enum equivalent in SQL Server)
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Products]') AND name = N'ScheduleClass'
)
BEGIN
    ALTER TABLE [Products] ADD [ScheduleClass] NVARCHAR(50) NULL;
END;
GO
