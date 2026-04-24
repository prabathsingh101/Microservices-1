-- SQL Script to create WarehouseStocks table for DB-First approach
-- This table tracks real-time stock levels per warehouse and product.

CREATE TABLE [dbo].[WarehouseStocks] (
    [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    [CompanyId] UNIQUEIDENTIFIER NOT NULL,
    [BranchId] UNIQUEIDENTIFIER NULL,
    [ProductId] UNIQUEIDENTIFIER NOT NULL,
    [WarehouseId] UNIQUEIDENTIFIER NOT NULL,
    [Quantity] DECIMAL(18, 2) NOT NULL DEFAULT 0,
    [MinStock] DECIMAL(18, 2) NOT NULL DEFAULT 0,
    [CreatedOn] DATETIME2 NULL,
    [CreatedBy] NVARCHAR(MAX) NULL,
    [ModifiedOn] DATETIME2 NULL,
    [ModifiedBy] NVARCHAR(MAX) NULL
);

-- Index for performance on common queries (Product/Warehouse/Tenant filters)
CREATE INDEX [IX_WarehouseStock_Main] ON [dbo].[WarehouseStocks] (
    [ProductId], 
    [WarehouseId], 
    [CompanyId], 
    [BranchId]
);

-- Foreign Key for Products
ALTER TABLE [dbo].[WarehouseStocks] WITH CHECK ADD CONSTRAINT [FK_WarehouseStocks_Products_ProductId] 
FOREIGN KEY([ProductId]) REFERENCES [dbo].[Products] ([Id]) ON DELETE CASCADE;

-- Foreign Key for Warehouses
ALTER TABLE [dbo].[WarehouseStocks] WITH CHECK ADD CONSTRAINT [FK_WarehouseStocks_Warehouses_WarehouseId] 
FOREIGN KEY([WarehouseId]) REFERENCES [dbo].[Warehouses] ([Id]) ON DELETE CASCADE;
