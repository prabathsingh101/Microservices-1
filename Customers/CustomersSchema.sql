CREATE TABLE [CustomerLedgers] (
    [Id] uniqueidentifier NOT NULL,
    [CustomerId] uniqueidentifier NOT NULL,
    [TransactionType] nvarchar(50) NOT NULL,
    [ReferenceId] nvarchar(max) NOT NULL,
    [Debit] decimal(18,2) NOT NULL,
    [Credit] decimal(18,2) NOT NULL,
    [Balance] decimal(18,2) NOT NULL,
    [TransactionDate] datetime2 NOT NULL,
    [Description] nvarchar(max) NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_CustomerLedgers] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [CustomerReceipts] (
    [Id] uniqueidentifier NOT NULL,
    [CustomerId] uniqueidentifier NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [ReceiptDate] datetime2 NOT NULL,
    [ReceiptMode] nvarchar(50) NOT NULL,
    [ReferenceNumber] nvarchar(max) NULL,
    [Remarks] nvarchar(max) NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_CustomerReceipts] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Customers] (
    [Id] uniqueidentifier NOT NULL,
    [CustomerName] nvarchar(200) NOT NULL,
    [CustomerType] nvarchar(max) NOT NULL,
    [Phone] nvarchar(20) NOT NULL,
    [Email] nvarchar(200) NULL,
    [GstNumber] nvarchar(50) NULL,
    [CreditLimit] decimal(18,2) NULL,
    [BillingAddress] nvarchar(500) NULL,
    [ShippingAddress] nvarchar(500) NULL,
    [Status] nvarchar(20) NULL,
    [CompanyId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_Customers] PRIMARY KEY ([Id])
);
GO


