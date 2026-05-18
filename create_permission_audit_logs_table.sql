-- 📜 Create PermissionAuditLogs Table 📜
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PermissionAuditLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PermissionAuditLogs] (
        [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
        [ActionByUserId] UNIQUEIDENTIFIER NOT NULL,
        [ActionByUserName] NVARCHAR(100) NOT NULL,
        [TargetUserId] UNIQUEIDENTIFIER NULL,
        [TargetUserName] NVARCHAR(100) NULL,
        [TargetRoleId] UNIQUEIDENTIFIER NULL,
        [TargetRoleName] NVARCHAR(100) NULL,
        [Action] NVARCHAR(50) NOT NULL,
        [Details] NVARCHAR(MAX) NOT NULL,
        [CompanyId] UNIQUEIDENTIFIER NULL,
        [CreatedBy] NVARCHAR(100) NULL,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [LastModifiedBy] NVARCHAR(100) NULL,
        [LastModifiedDate] DATETIME2 NULL,
        CONSTRAINT [PK_PermissionAuditLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    PRINT 'PermissionAuditLogs table created successfully.';
END
ELSE
BEGIN
    PRINT 'PermissionAuditLogs table already exists.';
END
GO
