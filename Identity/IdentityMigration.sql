IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Roles] (
    [Id] int NOT NULL IDENTITY,
    [RoleName] nvarchar(50) NOT NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
);

CREATE TABLE [Users] (
    [Id] uniqueidentifier NOT NULL,
    [UserName] nvarchar(100) NOT NULL,
    [Email] nvarchar(200) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ModifiedAt] datetime2 NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

CREATE TABLE [RefreshTokens] (
    [Id] uniqueidentifier NOT NULL,
    [Token] nvarchar(200) NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [IsRevoked] bit NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [UserRoles] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [RoleId] int NOT NULL,
    CONSTRAINT [PK_UserRoles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'RoleName') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] ON;
INSERT INTO [Roles] ([Id], [RoleName])
VALUES (1, N'Admin'),
(2, N'Teacher'),
(3, N'Student'),
(4, N'Parent'),
(5, N'Employee');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'RoleName') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] OFF;

CREATE UNIQUE INDEX [IX_RefreshTokens_Token] ON [RefreshTokens] ([Token]);

CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);

CREATE UNIQUE INDEX [IX_Roles_RoleName] ON [Roles] ([RoleName]);

CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);

CREATE UNIQUE INDEX [IX_UserRoles_UserId_RoleId] ON [UserRoles] ([UserId], [RoleId]);

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251229095414_addseedidIdentityCreate', N'10.0.3');

COMMIT;
GO

BEGIN TRANSACTION;
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251229111928_newmigIdentityCreate', N'10.0.3');

COMMIT;
GO

BEGIN TRANSACTION;
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260117073953_IdentityCreate', N'10.0.3');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [Menus] (
    [MenuId] int NOT NULL IDENTITY,
    [Title] nvarchar(max) NOT NULL,
    [Url] nvarchar(max) NOT NULL,
    [Icon] nvarchar(max) NULL,
    [ParentId] int NULL,
    [Order] int NOT NULL,
    [MenuId1] int NULL,
    CONSTRAINT [PK_Menus] PRIMARY KEY ([MenuId]),
    CONSTRAINT [FK_Menus_Menus_MenuId1] FOREIGN KEY ([MenuId1]) REFERENCES [Menus] ([MenuId])
);

CREATE TABLE [RolePermissions] (
    [RolePermissionId] int NOT NULL IDENTITY,
    [RoleId] int NOT NULL,
    [MenuId] int NOT NULL,
    [CanView] bit NOT NULL,
    [CanAdd] bit NOT NULL,
    [CanEdit] bit NOT NULL,
    [CanDelete] bit NOT NULL,
    CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([RolePermissionId]),
    CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Menus_MenuId1] ON [Menus] ([MenuId1]);

CREATE INDEX [IX_RolePermissions_RoleId] ON [RolePermissions] ([RoleId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260208160522_UserRole', N'10.0.3');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Menus] DROP CONSTRAINT [FK_Menus_Menus_MenuId1];

DROP INDEX [IX_Menus_MenuId1] ON [Menus];

DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Menus]') AND [c].[name] = N'MenuId1');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Menus] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [Menus] DROP COLUMN [MenuId1];

CREATE INDEX [IX_Menus_ParentId] ON [Menus] ([ParentId]);

ALTER TABLE [Menus] ADD CONSTRAINT [FK_Menus_Menus_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [Menus] ([MenuId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260208162328_UserPermissionRole', N'10.0.3');

COMMIT;
GO

BEGIN TRANSACTION;
EXEC sp_rename N'[RolePermissions].[RolePermissionId]', N'Id', 'COLUMN';

EXEC sp_rename N'[Menus].[MenuId]', N'Id', 'COLUMN';

ALTER TABLE [Menus] ADD [CreatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';

ALTER TABLE [Menus] ADD [ModifiedAt] datetime2 NULL;

CREATE INDEX [IX_RolePermissions_MenuId] ON [RolePermissions] ([MenuId]);

ALTER TABLE [RolePermissions] ADD CONSTRAINT [FK_RolePermissions_Menus_MenuId] FOREIGN KEY ([MenuId]) REFERENCES [Menus] ([Id]) ON DELETE CASCADE;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260208164146_DynamicMenuAndPermissions', N'10.0.3');

COMMIT;
GO

BEGIN TRANSACTION;
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260208165601_DynamicMenusAndPermissions', N'10.0.3');

COMMIT;
GO

BEGIN TRANSACTION;
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260208191229_AddAuditToRolePermissions', N'10.0.3');

COMMIT;
GO

BEGIN TRANSACTION;
DECLARE @var1 nvarchar(max);
SELECT @var1 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'CreatedAt');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT ' + @var1 + ';');
ALTER TABLE [Users] DROP COLUMN [CreatedAt];

DECLARE @var2 nvarchar(max);
SELECT @var2 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'ModifiedAt');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT ' + @var2 + ';');
ALTER TABLE [Users] DROP COLUMN [ModifiedAt];

DECLARE @var3 nvarchar(max);
SELECT @var3 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Menus]') AND [c].[name] = N'CreatedAt');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Menus] DROP CONSTRAINT ' + @var3 + ';');
ALTER TABLE [Menus] DROP COLUMN [CreatedAt];

DECLARE @var4 nvarchar(max);
SELECT @var4 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Menus]') AND [c].[name] = N'ModifiedAt');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Menus] DROP CONSTRAINT ' + @var4 + ';');
ALTER TABLE [Menus] DROP COLUMN [ModifiedAt];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260209065107_ddAuditFieldsConfiguration', N'10.0.3');

COMMIT;
GO

BEGIN TRANSACTION;
UPDATE [Roles] SET [RoleName] = N'Super Admin'
WHERE [Id] = 5;
SELECT @@ROWCOUNT;


UPDATE [Roles] SET [RoleName] = N'Warehouse'
WHERE [Id] = 4;
SELECT @@ROWCOUNT;


UPDATE [Roles] SET [RoleName] = N'Employee'
WHERE [Id] = 3;
SELECT @@ROWCOUNT;


UPDATE [Roles] SET [RoleName] = N'User'
WHERE [Id] = 2;
SELECT @@ROWCOUNT;


INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260213170723_UpdateRoles', N'10.0.3');

COMMIT;
GO

BEGIN TRANSACTION;
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'RoleName') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] ON;
INSERT INTO [Roles] ([Id], [RoleName])
VALUES (6, N'Manager');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'RoleName') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] OFF;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260213173820_AddManagerRole', N'10.0.3');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Users] ADD [ResetToken] nvarchar(max) NULL;

ALTER TABLE [Users] ADD [ResetTokenExpires] datetime2 NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260219123440_AddSomeCols', N'10.0.3');

COMMIT;
GO

BEGIN TRANSACTION;
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260219124437_AddResetTokenFields', N'10.0.3');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [RolePermissions] ADD [AdditionalActions] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260319035515_AddAdditionalActionsToRolePermission', N'10.0.3');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [RolePrintSettings] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] int NOT NULL,
    [PageName] nvarchar(100) NOT NULL,
    [PrintFormat] nvarchar(20) NOT NULL,
    CONSTRAINT [PK_RolePrintSettings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RolePrintSettings_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_RolePrintSettings_RoleId] ON [RolePrintSettings] ([RoleId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260327044303_AddRolePrintSettings', N'10.0.3');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [Subscriptions] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [PlanType] nvarchar(20) NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    [PaymentTxnId] nvarchar(100) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Subscriptions] PRIMARY KEY ([Id])
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260412071328_AddSubscriptions', N'10.0.3');

COMMIT;
GO

BEGIN TRANSACTION;
EXEC sp_rename N'[Subscriptions].[UserId]', N'CompanyId', 'COLUMN';

ALTER TABLE [Users] ADD [CompanyId] uniqueidentifier NULL;

ALTER TABLE [Subscriptions] ADD [CompanyName] nvarchar(max) NOT NULL DEFAULT N'';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260412085032_CompanyLicensingSupport', N'10.0.3');

COMMIT;
GO

BEGIN TRANSACTION;
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'RoleName') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] ON;
INSERT INTO [Roles] ([Id], [RoleName])
VALUES (7, N'Customer');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'RoleName') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] OFF;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260412094514_AddCustomerRoleSeed', N'10.0.3');

COMMIT;
GO

BEGIN TRANSACTION;
DECLARE @var5 nvarchar(max);
SELECT @var5 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Subscriptions]') AND [c].[name] = N'CompanyName');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Subscriptions] DROP CONSTRAINT ' + @var5 + ';');
ALTER TABLE [Subscriptions] ALTER COLUMN [CompanyName] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260412100126_MakeCompanyNameNullable', N'10.0.3');

COMMIT;
GO

BEGIN TRANSACTION;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Roles]') AND name = 'CompanyId')
                BEGIN
                    ALTER TABLE [Roles] ADD [CompanyId] uniqueidentifier NULL
                END
            

UPDATE [Roles] SET [CompanyId] = NULL
WHERE [Id] = 1;
SELECT @@ROWCOUNT;


UPDATE [Roles] SET [CompanyId] = NULL
WHERE [Id] = 2;
SELECT @@ROWCOUNT;


UPDATE [Roles] SET [CompanyId] = NULL
WHERE [Id] = 3;
SELECT @@ROWCOUNT;


UPDATE [Roles] SET [CompanyId] = NULL
WHERE [Id] = 4;
SELECT @@ROWCOUNT;


UPDATE [Roles] SET [CompanyId] = NULL
WHERE [Id] = 5;
SELECT @@ROWCOUNT;


UPDATE [Roles] SET [CompanyId] = NULL
WHERE [Id] = 6;
SELECT @@ROWCOUNT;


UPDATE [Roles] SET [CompanyId] = NULL
WHERE [Id] = 7;
SELECT @@ROWCOUNT;


INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260412110825_UpdateSubscriptionSchema', N'10.0.3');

COMMIT;
GO

BEGIN TRANSACTION;

                DELETE FROM Subscriptions 
                WHERE Id NOT IN (
                    SELECT TOP 1 Id FROM Subscriptions 
                    WHERE CompanyName = 'Krishna Mart' 
                    ORDER BY CreatedAt DESC
                )
            

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260412113612_CleanupDuplicateSubscriptions', N'10.0.3');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [RolePermissions] ADD [CompanyId] uniqueidentifier NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260413042342_AddCompanyIdToRolePermissions', N'10.0.3');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [UserRoles] ADD [CompanyId] uniqueidentifier NULL;

ALTER TABLE [RolePrintSettings] ADD [CompanyId] uniqueidentifier NULL;

ALTER TABLE [RefreshTokens] ADD [CompanyId] uniqueidentifier NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260413042758_FinalizeMultiTenancySchema', N'10.0.3');

COMMIT;
GO

