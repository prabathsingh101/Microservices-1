-- ==========================================
-- 1. SETUP MENUS IN IdentityDb
-- ==========================================
USE IdentityDb;
GO

DECLARE @FinanceParentId UNIQUEIDENTIFIER;
DECLARE @SalesSubParentId UNIQUEIDENTIFIER;

-- Find Finance menu
SELECT @FinanceParentId = Id FROM Menus WHERE Title = 'Finance' AND ParentId IS NULL;

IF @FinanceParentId IS NOT NULL
BEGIN
    -- Find or create 'Sales' sub-parent menu under Finance
    SELECT @SalesSubParentId = Id FROM Menus WHERE Title = 'Sales' AND ParentId = @FinanceParentId;

    IF @SalesSubParentId IS NULL
    BEGIN
        SET @SalesSubParentId = NEWID();
        
        DECLARE @SalesOrder INT;
        SELECT @SalesOrder = ISNULL(MAX([Order]), 0) + 1 FROM Menus WHERE ParentId = @FinanceParentId;

        INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate)
        VALUES (@SalesSubParentId, 'Sales', '#', 'shopping_cart', @FinanceParentId, @SalesOrder, GETDATE());
        
        PRINT 'Sales sub-parent menu created under Finance.';
    END

    -- Find or create 'Delivery Challan' menu under 'Sales'
    DECLARE @DcMenuId UNIQUEIDENTIFIER;
    SELECT @DcMenuId = Id FROM Menus WHERE Title = 'Delivery Challan' AND ParentId = @SalesSubParentId;

    IF @DcMenuId IS NULL
    BEGIN
        SET @DcMenuId = NEWID();
        
        DECLARE @DcOrder INT;
        SELECT @DcOrder = ISNULL(MAX([Order]), 0) + 1 FROM Menus WHERE ParentId = @SalesSubParentId;

        INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate)
        VALUES (@DcMenuId, 'Delivery Challan', '/app/finance/delivery-challan', 'local_shipping', @SalesSubParentId, @DcOrder, GETDATE());
        
        PRINT 'Delivery Challan menu created under Sales.';

        -- Assign permissions to Admin roles
        INSERT INTO RolePermissions (Id, RoleId, MenuId, CanView, CanAdd, CanEdit, CanDelete, CompanyId, AdditionalActions, CreatedDate)
        SELECT NEWID(), r.Id, @DcMenuId, 1, 1, 1, 1, NULL, 'PRINT,APPROVE,REJECT,EXPORT', GETDATE()
        FROM Roles r
        WHERE r.RoleName IN ('Admin', 'Super Admin', 'Default Admin')
          AND NOT EXISTS (SELECT 1 FROM RolePermissions rp WHERE rp.RoleId = r.Id AND rp.MenuId = @DcMenuId);

        PRINT 'Permissions assigned to Admin roles.';
    END
    ELSE
    BEGIN
        PRINT 'Delivery Challan menu already exists.';
    END
END
ELSE
BEGIN
    PRINT 'Error: Could not find Finance parent menu.';
END
GO


-- ==========================================
-- 2. CREATE TRANSACTION TABLES IN InventoryDb
-- ==========================================
USE InventoryDb;
GO

-- Create DeliveryChallans Table (All columns nullable except PK)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DeliveryChallans]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DeliveryChallans] (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [ChallanNo] NVARCHAR(50) NULL,
        [ChallanDate] DATETIME2 NULL,
        [CustomerId] UNIQUEIDENTIFIER NULL,
        [CustomerName] NVARCHAR(255) NULL,
        [SubTotal] DECIMAL(18,2) NULL,
        [TotalTax] DECIMAL(18,2) NULL,
        [GrandTotal] DECIMAL(18,2) NULL,
        [Remarks] NVARCHAR(MAX) NULL,
        [Status] NVARCHAR(50) NULL, -- Pending, Invoiced, Cancelled
        [GrossWeight] DECIMAL(18,2) NULL,
        [VehicleRegNo] NVARCHAR(100) NULL,
        [Origin] NVARCHAR(255) NULL,
        [Destination] NVARCHAR(255) NULL,
        [CompanyId] UNIQUEIDENTIFIER NULL,
        [BranchId] NVARCHAR(MAX) NULL,
        [CreatedOn] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(MAX) NULL,
        [ModifiedOn] DATETIME2 NULL,
        [ModifiedBy] NVARCHAR(MAX) NULL,
        CONSTRAINT [PK_DeliveryChallans] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    
    CREATE INDEX [IX_DeliveryChallans_ChallanNo] ON [DeliveryChallans] ([ChallanNo]);
    PRINT 'DeliveryChallans table created.';
END
ELSE
BEGIN
    -- If table exists, make sure columns are nullable (alter statements if necessary)
    PRINT 'DeliveryChallans table already exists.';
END
GO

-- Create DeliveryChallanItems Table (All columns nullable except PK)
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DeliveryChallanItems]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DeliveryChallanItems] (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [DeliveryChallanId] UNIQUEIDENTIFIER NULL,
        [ProductId] UNIQUEIDENTIFIER NULL,
        [ProductName] NVARCHAR(255) NULL,
        [Qty] DECIMAL(18,2) NULL,
        [Unit] NVARCHAR(50) NULL,
        [Rate] DECIMAL(18,2) NULL,
        [MRP] DECIMAL(18,2) NULL,
        [DiscountPercent] DECIMAL(18,2) NULL,
        [DiscountAmount] DECIMAL(18,2) NULL,
        [GSTPercent] DECIMAL(18,2) NULL,
        [TaxAmount] DECIMAL(18,2) NULL,
        [Total] DECIMAL(18,2) NULL,
        [WarehouseId] UNIQUEIDENTIFIER NULL,
        [RackId] UNIQUEIDENTIFIER NULL,
        [BatchNumber] NVARCHAR(100) NULL,
        [MfgDate] DATETIME2 NULL,
        [ExpDate] DATETIME2 NULL,
        [CompanyId] UNIQUEIDENTIFIER NULL,
        [BranchId] NVARCHAR(MAX) NULL,
        [CreatedOn] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(MAX) NULL,
        [ModifiedOn] DATETIME2 NULL,
        [ModifiedBy] NVARCHAR(MAX) NULL,
        CONSTRAINT [PK_DeliveryChallanItems] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_DeliveryChallanItems_DeliveryChallans_DeliveryChallanId] FOREIGN KEY ([DeliveryChallanId]) REFERENCES [dbo].[DeliveryChallans] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DeliveryChallanItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DeliveryChallanItems_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [dbo].[Warehouses] ([Id]),
        CONSTRAINT [FK_DeliveryChallanItems_Racks_RackId] FOREIGN KEY ([RackId]) REFERENCES [dbo].[Racks] ([Id])
    );
    
    CREATE INDEX [IX_DeliveryChallanItems_DeliveryChallanId] ON [DeliveryChallanItems] ([DeliveryChallanId]);
    PRINT 'DeliveryChallanItems table created.';
END
ELSE
BEGIN
    PRINT 'DeliveryChallanItems table already exists.';
END
GO

-- Add link column in SalesInvoices table to point to DeliveryChallan
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[dbo].[SalesInvoices]') AND name = 'DeliveryChallanId')
BEGIN
    ALTER TABLE [dbo].[SalesInvoices] ADD [DeliveryChallanId] UNIQUEIDENTIFIER NULL;
    PRINT 'DeliveryChallanId column added to SalesInvoices table.';
END
ELSE
BEGIN
    PRINT 'DeliveryChallanId column already exists in SalesInvoices table.';
END
GO
