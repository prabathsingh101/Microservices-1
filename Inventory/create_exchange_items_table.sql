-- =========================================================================
-- Author:      Antigravity
-- Create Date: 2026-06-14
-- Description: Database updates to support Sale Exchange Items in InventoryDb.
--              Run this script directly on the SQL Server.
-- =========================================================================

-- 1. Create SaleExchangeItems Table
IF OBJECT_ID(N'[dbo].[SaleExchangeItems]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SaleExchangeItems] (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [SaleReturnHeaderId] UNIQUEIDENTIFIER NOT NULL,
        [ProductId] UNIQUEIDENTIFIER NOT NULL,
        [Qty] DECIMAL(18, 2) NOT NULL,
        [UnitPrice] DECIMAL(18, 2) NOT NULL,
        [DiscountPercent] DECIMAL(18, 2) NOT NULL,
        [DiscountAmount] DECIMAL(18, 2) NOT NULL,
        [TaxPercentage] DECIMAL(18, 2) NOT NULL,
        [TaxAmount] DECIMAL(18, 2) NOT NULL,
        [TotalAmount] DECIMAL(18, 2) NOT NULL,
        
        -- Batch, Expiry & Location tracking
        [MfgDate] DATETIME2(7) NULL,
        [ExpDate] DATETIME2(7) NULL,
        [WarehouseId] UNIQUEIDENTIFIER NULL,
        [RackId] UNIQUEIDENTIFIER NULL,
        [BatchNumber] NVARCHAR(150) NULL,
        [ReferenceNumber] NVARCHAR(150) NULL,
        
        -- Tenant and Audit properties
        [CompanyId] UNIQUEIDENTIFIER NOT NULL,
        [BranchId] NVARCHAR(100) NULL,
        [CreatedOn] DATETIME2(7) NULL,
        [CreatedBy] NVARCHAR(150) NULL,
        [ModifiedOn] DATETIME2(7) NULL,
        [ModifiedBy] NVARCHAR(150) NULL,

        CONSTRAINT [PK_SaleExchangeItems] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_SaleExchangeItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_SaleExchangeItems_SaleReturnHeaders_SaleReturnHeaderId] FOREIGN KEY ([SaleReturnHeaderId]) REFERENCES [dbo].[SaleReturnHeaders] ([Id]) ON DELETE CASCADE
    )
    PRINT 'Table [dbo].[SaleExchangeItems] created successfully.'
END
ELSE
BEGIN
    PRINT 'Table [dbo].[SaleExchangeItems] already exists.'
END
GO

-- 2. Add TotalReturnAmount column to SaleReturnHeaders
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SaleReturnHeaders]') AND name = N'TotalReturnAmount')
BEGIN
    ALTER TABLE [dbo].[SaleReturnHeaders] ADD [TotalReturnAmount] DECIMAL(18, 2) NOT NULL DEFAULT 0.00;
    PRINT 'Column [TotalReturnAmount] added to [dbo].[SaleReturnHeaders].'
END
ELSE
BEGIN
    PRINT 'Column [TotalReturnAmount] already exists in [dbo].[SaleReturnHeaders].'
END
GO

-- 3. Add TotalExchangeAmount column to SaleReturnHeaders
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SaleReturnHeaders]') AND name = N'TotalExchangeAmount')
BEGIN
    ALTER TABLE [dbo].[SaleReturnHeaders] ADD [TotalExchangeAmount] DECIMAL(18, 2) NOT NULL DEFAULT 0.00;
    PRINT 'Column [TotalExchangeAmount] added to [dbo].[SaleReturnHeaders].'
END
ELSE
BEGIN
    PRINT 'Column [TotalExchangeAmount] already exists in [dbo].[SaleReturnHeaders].'
END
GO

-- 4. Add ReturnMode column to SaleReturnHeaders
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SaleReturnHeaders]') AND name = N'ReturnMode')
BEGIN
    ALTER TABLE [dbo].[SaleReturnHeaders] ADD [ReturnMode] NVARCHAR(50) NOT NULL DEFAULT 'RefundOnly';
    PRINT 'Column [ReturnMode] added to [dbo].[SaleReturnHeaders].'
END
ELSE
BEGIN
    PRINT 'Column [ReturnMode] already exists in [dbo].[SaleReturnHeaders].'
END
GO
