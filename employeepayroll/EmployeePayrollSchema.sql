CREATE TABLE [Employees] (
    [Id] uniqueidentifier NOT NULL,
    [EmployeeCode] nvarchar(max) NOT NULL,
    [FullName] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NULL,
    [Phone] nvarchar(max) NULL,
    [Designation] nvarchar(max) NULL,
    [Department] nvarchar(max) NULL,
    [DateOfJoining] datetime2 NOT NULL,
    [ProfilePicture] nvarchar(max) NULL,
    [BasicSalary] decimal(18,2) NOT NULL,
    [HRA] decimal(18,2) NOT NULL,
    [Conveyance] decimal(18,2) NOT NULL,
    [SpecialAllowance] decimal(18,2) NOT NULL,
    [PF] decimal(18,2) NOT NULL,
    [Tax] decimal(18,2) NOT NULL,
    [IsActive] bit NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] uniqueidentifier NULL,
    CONSTRAINT [PK_Employees] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Holidays] (
    [Id] uniqueidentifier NOT NULL,
    [HolidayName] nvarchar(max) NOT NULL,
    [Date] datetime2 NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsRecursive] bit NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] uniqueidentifier NULL,
    CONSTRAINT [PK_Holidays] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Attendances] (
    [Id] uniqueidentifier NOT NULL,
    [EmployeeId] uniqueidentifier NOT NULL,
    [Date] datetime2 NOT NULL,
    [CheckIn] datetime2 NULL,
    [CheckOut] datetime2 NULL,
    [Status] int NOT NULL,
    [Method] int NOT NULL,
    [Remarks] nvarchar(max) NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] uniqueidentifier NULL,
    CONSTRAINT [PK_Attendances] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Attendances_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Leaves] (
    [Id] uniqueidentifier NOT NULL,
    [EmployeeId] uniqueidentifier NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [Type] int NOT NULL,
    [Reason] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [AdminRemarks] nvarchar(max) NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] uniqueidentifier NULL,
    CONSTRAINT [PK_Leaves] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Leaves_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [SalarySlips] (
    [Id] uniqueidentifier NOT NULL,
    [EmployeeId] uniqueidentifier NOT NULL,
    [Month] int NOT NULL,
    [Year] int NOT NULL,
    [EmployeeNameSnapshot] nvarchar(max) NULL,
    [EmployeeCodeSnapshot] nvarchar(max) NULL,
    [DesignationSnapshot] nvarchar(max) NULL,
    [BasicSalary] decimal(18,2) NOT NULL,
    [HRA] decimal(18,2) NOT NULL,
    [Conveyance] decimal(18,2) NOT NULL,
    [SpecialAllowance] decimal(18,2) NOT NULL,
    [PF] decimal(18,2) NOT NULL,
    [Tax] decimal(18,2) NOT NULL,
    [GrossEarning] decimal(18,2) NOT NULL,
    [TotalDeduction] decimal(18,2) NOT NULL,
    [NetSalary] decimal(18,2) NOT NULL,
    [GeneratedDate] datetime2 NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] uniqueidentifier NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] uniqueidentifier NULL,
    CONSTRAINT [PK_SalarySlips] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SalarySlips_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE
);
GO


CREATE INDEX [IX_Attendances_EmployeeId] ON [Attendances] ([EmployeeId]);
GO


CREATE INDEX [IX_Leaves_EmployeeId] ON [Leaves] ([EmployeeId]);
GO


CREATE INDEX [IX_SalarySlips_EmployeeId] ON [SalarySlips] ([EmployeeId]);
GO


