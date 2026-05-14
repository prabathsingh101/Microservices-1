CREATE TABLE [Menus] (
    [Id] uniqueidentifier NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [Url] nvarchar(max) NOT NULL,
    [Icon] nvarchar(max) NULL,
    [ParentId] uniqueidentifier NULL,
    [Order] int NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedBy] nvarchar(max) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [LastModifiedBy] nvarchar(max) NULL,
    [LastModifiedDate] datetime2 NULL,
    CONSTRAINT [PK_Menus] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Menus_Menus_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [Menus] ([Id])
);
GO


CREATE TABLE [Roles] (
    [Id] uniqueidentifier NOT NULL,
    [RoleName] nvarchar(50) NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedBy] nvarchar(max) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [LastModifiedBy] nvarchar(max) NULL,
    [LastModifiedDate] datetime2 NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Subscriptions] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [CompanyCode] nvarchar(50) NOT NULL,
    [CompanyName] nvarchar(max) NULL,
    [CompanyTagline] nvarchar(max) NULL,
    [PlanType] nvarchar(20) NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [IsActive] bit NOT NULL,
    [PaymentTxnId] nvarchar(100) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [LastModifiedBy] nvarchar(max) NULL,
    [LastModifiedDate] datetime2 NULL,
    CONSTRAINT [PK_Subscriptions] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Users] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] nvarchar(max) NULL,
    [UserName] nvarchar(100) NOT NULL,
    [Email] nvarchar(200) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [ResetToken] nvarchar(max) NULL,
    [ResetTokenExpires] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [LastModifiedBy] nvarchar(max) NULL,
    [LastModifiedDate] datetime2 NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [RolePermissions] (
    [Id] uniqueidentifier NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    [MenuId] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] nvarchar(max) NULL,
    [CanView] bit NOT NULL,
    [CanAdd] bit NOT NULL,
    [CanEdit] bit NOT NULL,
    [CanDelete] bit NOT NULL,
    [AdditionalActions] nvarchar(max) NULL,
    [CreatedBy] nvarchar(max) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [LastModifiedBy] nvarchar(max) NULL,
    [LastModifiedDate] datetime2 NULL,
    CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RolePermissions_Menus_MenuId] FOREIGN KEY ([MenuId]) REFERENCES [Menus] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [RolePrintSettings] (
    [Id] uniqueidentifier NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] nvarchar(max) NULL,
    [PageName] nvarchar(100) NOT NULL,
    [PrintFormat] nvarchar(20) NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [LastModifiedBy] nvarchar(max) NULL,
    [LastModifiedDate] datetime2 NULL,
    CONSTRAINT [PK_RolePrintSettings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RolePrintSettings_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [RefreshTokens] (
    [Id] uniqueidentifier NOT NULL,
    [Token] nvarchar(200) NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [IsRevoked] bit NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedBy] nvarchar(max) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [LastModifiedBy] nvarchar(max) NULL,
    [LastModifiedDate] datetime2 NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [UserRoles] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedBy] nvarchar(max) NULL,
    [CreatedDate] datetime2 NOT NULL,
    [LastModifiedBy] nvarchar(max) NULL,
    [LastModifiedDate] datetime2 NULL,
    CONSTRAINT [PK_UserRoles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BranchId', N'CompanyId', N'CreatedBy', N'CreatedDate', N'LastModifiedBy', N'LastModifiedDate', N'RoleName') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] ON;
INSERT INTO [Roles] ([Id], [BranchId], [CompanyId], [CreatedBy], [CreatedDate], [LastModifiedBy], [LastModifiedDate], [RoleName])
VALUES ('00000000-0000-0000-0000-000000000001', NULL, NULL, NULL, '2026-04-30T14:57:03.0845296Z', NULL, NULL, N'Default Admin');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'BranchId', N'CompanyId', N'CreatedBy', N'CreatedDate', N'LastModifiedBy', N'LastModifiedDate', N'RoleName') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] OFF;
GO


CREATE INDEX [IX_Menus_ParentId] ON [Menus] ([ParentId]);
GO


CREATE UNIQUE INDEX [IX_RefreshTokens_Token] ON [RefreshTokens] ([Token]);
GO


CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
GO


CREATE INDEX [IX_RolePermissions_MenuId] ON [RolePermissions] ([MenuId]);
GO


CREATE INDEX [IX_RolePermissions_RoleId] ON [RolePermissions] ([RoleId]);
GO


CREATE INDEX [IX_RolePrintSettings_RoleId] ON [RolePrintSettings] ([RoleId]);
GO


CREATE UNIQUE INDEX [IX_Roles_RoleName_CompanyId] ON [Roles] ([RoleName], [CompanyId]) WHERE [CompanyId] IS NOT NULL;
GO


CREATE UNIQUE INDEX [IX_Subscriptions_CompanyCode] ON [Subscriptions] ([CompanyCode]);
GO


CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);
GO


CREATE UNIQUE INDEX [IX_UserRoles_UserId_RoleId] ON [UserRoles] ([UserId], [RoleId]);
GO


CREATE UNIQUE INDEX [IX_Users_Email_CompanyId] ON [Users] ([Email], [CompanyId]) WHERE [CompanyId] IS NOT NULL;
GO





