-- =========================================================================
-- Author:      Antigravity
-- Create Date: 2026-06-13
-- Description: Create tables for RFQ (Request for Quotation) integration.
--              Compatible with EF Core DB-First Scaffolding.
-- =========================================================================

-- 1. Create RequestForQuotations Table
IF OBJECT_ID(N'[dbo].[RequestForQuotations]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RequestForQuotations] (
        [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_RequestForQuotations_Id] DEFAULT (NEWID()),
        [RfqNo] NVARCHAR(50) NOT NULL,
        [SupplierId] UNIQUEIDENTIFIER NOT NULL,
        [SupplierName] NVARCHAR(150) NULL,
        [CreatedDate] DATETIME2(7) NOT NULL CONSTRAINT [DF_RequestForQuotations_CreatedDate] DEFAULT (GETUTCDATE()),
        [ExpiryDate] DATETIME2(7) NULL,
        [Status] INT NOT NULL, -- Enum: Draft = 1, Sent = 2, Confirmed = 3, Converted = 4
        [Remarks] NVARCHAR(500) NULL,
        
        -- Inherited Audit/Tenant properties from BaseAuditableEntity
        [CompanyId] UNIQUEIDENTIFIER NOT NULL,
        [BranchId] NVARCHAR(100) NULL,
        [CreatedOn] DATETIME2(7) NULL,
        [CreatedBy] NVARCHAR(150) NULL,
        [ModifiedOn] DATETIME2(7) NULL,
        [ModifiedBy] NVARCHAR(150) NULL,

        CONSTRAINT [PK_RequestForQuotations] PRIMARY KEY CLUSTERED ([Id] ASC)
    )
    PRINT 'Table [dbo].[RequestForQuotations] created successfully.'
END
ELSE
BEGIN
    PRINT 'Table [dbo].[RequestForQuotations] already exists.'
END
GO

-- 2. Create Unique Index on RequestForQuotations
IF EXISTS (SELECT * FROM sys.tables WHERE name = N'RequestForQuotations' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_RequestForQuotations_CompanyId_RfqNo' AND object_id = OBJECT_ID(N'[dbo].[RequestForQuotations]'))
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX [IX_RequestForQuotations_CompanyId_RfqNo]
            ON [dbo].[RequestForQuotations]([CompanyId] ASC, [RfqNo] ASC)
        PRINT 'Index [IX_RequestForQuotations_CompanyId_RfqNo] created successfully.'
    END
END
GO

-- 3. Create RequestForQuotationItems Table
IF OBJECT_ID(N'[dbo].[RequestForQuotationItems]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[RequestForQuotationItems] (
        [Id] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_RequestForQuotationItems_Id] DEFAULT (NEWID()),
        [RfqId] UNIQUEIDENTIFIER NOT NULL,
        [ProductId] UNIQUEIDENTIFIER NOT NULL,
        [Qty] DECIMAL(18, 2) NOT NULL,
        [UnitPrice] DECIMAL(18, 2) NULL,
        [TaxRate] DECIMAL(18, 2) NULL,
        [Discount] DECIMAL(18, 2) NULL,
        [TotalCost] DECIMAL(18, 2) NULL,
        
        -- Inherited Audit/Tenant properties from BaseAuditableEntity
        [CompanyId] UNIQUEIDENTIFIER NOT NULL,
        [BranchId] NVARCHAR(100) NULL,
        [CreatedOn] DATETIME2(7) NULL,
        [CreatedBy] NVARCHAR(150) NULL,
        [ModifiedOn] DATETIME2(7) NULL,
        [ModifiedBy] NVARCHAR(150) NULL,

        CONSTRAINT [PK_RequestForQuotationItems] PRIMARY KEY CLUSTERED ([Id] ASC),
        
        -- Foreign Key: RequestForQuotationItem -> RequestForQuotation (Cascade Delete)
        CONSTRAINT [FK_RequestForQuotationItems_RequestForQuotations_RfqId] FOREIGN KEY ([RfqId]) 
            REFERENCES [dbo].[RequestForQuotations] ([Id]) ON DELETE CASCADE,
            
        -- Foreign Key: RequestForQuotationItem -> Product (No Action)
        CONSTRAINT [FK_RequestForQuotationItems_Products_ProductId] FOREIGN KEY ([ProductId]) 
            REFERENCES [dbo].[Products] ([Id]) ON DELETE NO ACTION
    )
    PRINT 'Table [dbo].[RequestForQuotationItems] created successfully.'
END
ELSE
BEGIN
    PRINT 'Table [dbo].[RequestForQuotationItems] already exists.'
END
GO

-- 4. Create Index on RequestForQuotationItems
IF EXISTS (SELECT * FROM sys.tables WHERE name = N'RequestForQuotationItems' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_RequestForQuotationItems_RfqId' AND object_id = OBJECT_ID(N'[dbo].[RequestForQuotationItems]'))
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_RequestForQuotationItems_RfqId]
            ON [dbo].[RequestForQuotationItems]([RfqId] ASC)
        PRINT 'Index [IX_RequestForQuotationItems_RfqId] created successfully.'
    END
END
GO
