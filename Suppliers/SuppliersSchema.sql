CREATE TABLE [SupplierLedgers] (
    [Id] uniqueidentifier NOT NULL,
    [SupplierId] uniqueidentifier NOT NULL,
    [TransactionType] nvarchar(50) NOT NULL,
    [ReferenceId] nvarchar(max) NOT NULL,
    [Debit] decimal(18,2) NOT NULL,
    [Credit] decimal(18,2) NOT NULL,
    [Balance] decimal(18,2) NOT NULL,
    [TransactionDate] datetime2 NOT NULL,
    [Description] nvarchar(max) NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_SupplierLedgers] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [SupplierPayments] (
    [Id] uniqueidentifier NOT NULL,
    [SupplierId] uniqueidentifier NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [PaymentDate] datetime2 NOT NULL,
    [PaymentMode] nvarchar(50) NOT NULL,
    [ReferenceNumber] nvarchar(max) NULL,
    [Remarks] nvarchar(max) NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_SupplierPayments] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Suppliers] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Phone] nvarchar(15) NOT NULL,
    [GstIn] nvarchar(max) NULL,
    [Address] nvarchar(max) NULL,
    [Email] nvarchar(max) NULL,
    [DefaultPriceListId] uniqueidentifier NULL,
    [IsActive] bit NOT NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_Suppliers] PRIMARY KEY ([Id])
);
GO


