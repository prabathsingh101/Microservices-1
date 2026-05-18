USE IdentityDb;
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PermissionAuditLogs]') AND name = 'BranchId')
BEGIN
    ALTER TABLE [dbo].[PermissionAuditLogs] ADD [BranchId] NVARCHAR(100) NULL;
    PRINT 'BranchId column added successfully.';
END
GO
