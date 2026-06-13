-- ==========================================
-- CREATE RFQ SCHEMA TABLES & INDEXES
-- ==========================================

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RequestForQuotations]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RequestForQuotations] (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [RfqNo] NVARCHAR(50) NOT NULL,
        [SupplierId] UNIQUEIDENTIFIER NOT NULL,
        [SupplierName] NVARCHAR(250) NULL,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT (GETUTCDATE()),
        [ExpiryDate] DATETIME2 NULL,
        [Status] INT NOT NULL DEFAULT (1), -- 1 = Draft
        [Remarks] NVARCHAR(MAX) NULL,
        [CompanyId] UNIQUEIDENTIFIER NOT NULL,
        [BranchId] NVARCHAR(50) NULL,
        [CreatedOn] DATETIME2 NOT NULL DEFAULT (GETUTCDATE()),
        [CreatedBy] NVARCHAR(250) NULL,
        [ModifiedOn] DATETIME2 NULL,
        [ModifiedBy] NVARCHAR(250) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT (0),
        CONSTRAINT [PK_RequestForQuotations] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_RequestForQuotations_CompanyId_RfqNo' AND object_id = OBJECT_ID(N'[dbo].[RequestForQuotations]'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [IX_RequestForQuotations_CompanyId_RfqNo]
        ON [dbo].[RequestForQuotations]([CompanyId] ASC, [RfqNo] ASC)
        WHERE [IsDeleted] = 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RequestForQuotationItems]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RequestForQuotationItems] (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [RfqId] UNIQUEIDENTIFIER NOT NULL,
        [ProductId] UNIQUEIDENTIFIER NOT NULL,
        [Qty] DECIMAL(18, 4) NOT NULL,
        [UnitPrice] DECIMAL(18, 4) NULL,
        [TaxRate] DECIMAL(18, 4) NULL,
        [Discount] DECIMAL(18, 4) NULL,
        [TotalCost] DECIMAL(18, 4) NULL,
        [CompanyId] UNIQUEIDENTIFIER NOT NULL,
        [BranchId] NVARCHAR(50) NULL,
        [CreatedOn] DATETIME2 NOT NULL DEFAULT (GETUTCDATE()),
        [CreatedBy] NVARCHAR(250) NULL,
        [ModifiedOn] DATETIME2 NULL,
        [ModifiedBy] NVARCHAR(250) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT (0),
        CONSTRAINT [PK_RequestForQuotationItems] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_RequestForQuotationItems_RequestForQuotations_RfqId] FOREIGN KEY ([RfqId]) REFERENCES [dbo].[RequestForQuotations] ([Id]) ON DELETE CASCADE
    );
END
GO
