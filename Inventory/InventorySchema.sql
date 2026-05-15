CREATE TABLE [AppNotifications] (
    [Id] uniqueidentifier NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [Type] nvarchar(max) NOT NULL,
    [IsRead] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [TargetUrl] nvarchar(max) NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_AppNotifications] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Categories] (
    [Id] uniqueidentifier NOT NULL,
    [CategoryCode] nvarchar(50) NULL,
    [CategoryName] nvarchar(150) NOT NULL,
    [DefaultGst] decimal(5,2) NOT NULL,
    [Description] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [ParentCategoryId] uniqueidentifier NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Categories_Categories_ParentCategoryId] FOREIGN KEY ([ParentCategoryId]) REFERENCES [Categories] ([Id])
);
GO


CREATE TABLE [ExpenseCategories] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_ExpenseCategories] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [GatePasses] (
    [Id] uniqueidentifier NOT NULL,
    [PassNo] nvarchar(50) NOT NULL,
    [PassType] nvarchar(max) NOT NULL,
    [ReferenceType] int NOT NULL,
    [ReferenceId] nvarchar(max) NOT NULL,
    [ReferenceNo] nvarchar(max) NOT NULL,
    [InvoiceNo] nvarchar(max) NULL,
    [PartyName] nvarchar(max) NOT NULL,
    [VehicleNo] nvarchar(max) NOT NULL,
    [VehicleType] nvarchar(max) NULL,
    [DriverName] nvarchar(max) NOT NULL,
    [DriverPhone] nvarchar(max) NOT NULL,
    [TransporterName] nvarchar(max) NULL,
    [TotalQty] decimal(18,2) NOT NULL,
    [TotalWeight] decimal(18,2) NULL,
    [GateEntryTime] datetime2 NOT NULL,
    [SecurityGuard] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [Remarks] nvarchar(max) NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_GatePasses] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [InventoryTransactions] (
    [Id] uniqueidentifier NOT NULL,
    [ProductId] uniqueidentifier NOT NULL,
    [Quantity] decimal(18,2) NOT NULL,
    [TransactionType] nvarchar(50) NOT NULL,
    [ReferenceId] nvarchar(50) NOT NULL,
    [WarehouseId] uniqueidentifier NULL,
    [RackId] uniqueidentifier NULL,
    [MfgDate] datetime2 NULL,
    [ExpDate] datetime2 NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedOn] datetime2 NOT NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_InventoryTransactions] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [PriceLists] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Code] nvarchar(50) NOT NULL,
    [PriceType] nvarchar(max) NOT NULL,
    [ApplicableGroup] nvarchar(max) NOT NULL,
    [Currency] nvarchar(max) NOT NULL,
    [Remarks] nvarchar(max) NULL,
    [ValidFrom] datetime2 NOT NULL,
    [ValidTo] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_PriceLists] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [PurchaseReturns] (
    [Id] uniqueidentifier NOT NULL,
    [ReturnNumber] nvarchar(50) NOT NULL,
    [ReturnDate] datetime2 NOT NULL,
    [SupplierId] uniqueidentifier NOT NULL,
    [SubTotal] decimal(18,2) NOT NULL,
    [TotalTax] decimal(18,2) NOT NULL,
    [GrandTotal] decimal(18,2) NOT NULL,
    [Remarks] nvarchar(max) NOT NULL,
    [Status] nvarchar(max) NOT NULL DEFAULT N'Draft',
    [GatePassNo] nvarchar(max) NULL,
    [IsQuick] bit NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_PurchaseReturns] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [SaleOrders] (
    [Id] uniqueidentifier NOT NULL,
    [SONumber] nvarchar(50) NOT NULL,
    [CustomerId] uniqueidentifier NULL,
    [SODate] datetime2 NOT NULL,
    [ExpectedDeliveryDate] datetime2 NULL,
    [SubTotal] decimal(18,2) NOT NULL,
    [TotalTax] decimal(18,2) NOT NULL,
    [GrandTotal] decimal(18,2) NOT NULL,
    [TaxType] nvarchar(max) NULL,
    [TdsPercent] decimal(18,2) NULL,
    [TdsAmount] decimal(18,2) NULL,
    [TcsPercent] decimal(18,2) NULL,
    [TcsAmount] decimal(18,2) NULL,
    [IgstAmount] decimal(18,2) NULL,
    [CgstAmount] decimal(18,2) NULL,
    [SgstAmount] decimal(18,2) NULL,
    [Remarks] nvarchar(max) NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [GatePassNo] nvarchar(max) NULL,
    [IsQuick] bit NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_SaleOrders] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Units] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [IsActive] bit NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_Units] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Warehouses] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [City] nvarchar(100) NULL,
    [Description] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_Warehouses] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Subcategories] (
    [Id] uniqueidentifier NOT NULL,
    [CategoryId] uniqueidentifier NOT NULL,
    [SubcategoryCode] nvarchar(50) NULL,
    [SubcategoryName] nvarchar(150) NOT NULL,
    [DefaultGst] decimal(5,2) NOT NULL,
    [Description] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_Subcategories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Subcategories_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [ExpenseEntries] (
    [Id] uniqueidentifier NOT NULL,
    [CategoryId] uniqueidentifier NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [ExpenseDate] datetime2 NOT NULL,
    [PaymentMode] nvarchar(50) NOT NULL,
    [ReferenceNo] nvarchar(max) NULL,
    [Remarks] nvarchar(max) NULL,
    [AttachmentPath] nvarchar(max) NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_ExpenseEntries] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ExpenseEntries_ExpenseCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [ExpenseCategories] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [PurchaseOrders] (
    [Id] uniqueidentifier NOT NULL,
    [PoNumber] nvarchar(50) NOT NULL,
    [SupplierId] uniqueidentifier NOT NULL,
    [SupplierName] nvarchar(max) NULL,
    [PriceListId] uniqueidentifier NOT NULL,
    [PoDate] datetime2 NOT NULL,
    [ExpectedDeliveryDate] datetime2 NULL,
    [Remarks] nvarchar(max) NULL,
    [TotalQuantity] decimal(18,2) NOT NULL,
    [TotalTax] decimal(18,2) NOT NULL,
    [SubTotal] decimal(18,2) NOT NULL,
    [GrandTotal] decimal(18,2) NOT NULL,
    [TaxType] nvarchar(max) NULL,
    [TdsPercent] decimal(18,2) NULL,
    [TdsAmount] decimal(18,2) NULL,
    [TcsPercent] decimal(18,2) NULL,
    [TcsAmount] decimal(18,2) NULL,
    [IgstAmount] decimal(18,2) NULL,
    [CgstAmount] decimal(18,2) NULL,
    [SgstAmount] decimal(18,2) NULL,
    [Status] nvarchar(max) NOT NULL,
    [IsQuick] bit NOT NULL,
    [IsDispatched] bit NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_PurchaseOrders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PurchaseOrders_PriceLists_PriceListId] FOREIGN KEY ([PriceListId]) REFERENCES [PriceLists] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [PurchaseReturnItems] (
    [Id] uniqueidentifier NOT NULL,
    [PurchaseReturnId] uniqueidentifier NOT NULL,
    [ProductId] uniqueidentifier NOT NULL,
    [GrnRef] nvarchar(max) NOT NULL,
    [ReturnQty] decimal(18,2) NOT NULL,
    [Rate] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [GstPercent] decimal(18,2) NOT NULL,
    [DiscountPercent] decimal(18,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [MfgDate] datetime2 NULL,
    [ExpDate] datetime2 NULL,
    [WarehouseId] uniqueidentifier NULL,
    [RackId] uniqueidentifier NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_PurchaseReturnItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PurchaseReturnItems_PurchaseReturns_PurchaseReturnId] FOREIGN KEY ([PurchaseReturnId]) REFERENCES [PurchaseReturns] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [SaleReturnHeaders] (
    [Id] uniqueidentifier NOT NULL,
    [ReturnNumber] nvarchar(20) NOT NULL,
    [ReturnDate] datetime2 NOT NULL,
    [SaleOrderId] uniqueidentifier NOT NULL,
    [CustomerId] uniqueidentifier NULL,
    [SubTotal] decimal(18,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [DiscountAmount] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [Remarks] nvarchar(max) NULL,
    [Status] nvarchar(max) NOT NULL,
    [GatePassNo] nvarchar(max) NULL,
    [IsQuick] bit NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_SaleReturnHeaders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SaleReturnHeaders_SaleOrders_SaleOrderId] FOREIGN KEY ([SaleOrderId]) REFERENCES [SaleOrders] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Racks] (
    [Id] uniqueidentifier NOT NULL,
    [WarehouseId] uniqueidentifier NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_Racks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Racks_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [StockTransferHeaders] (
    [Id] uniqueidentifier NOT NULL,
    [TransferNumber] nvarchar(max) NOT NULL,
    [TransferDate] datetime2 NOT NULL,
    [FromWarehouseId] uniqueidentifier NOT NULL,
    [ToWarehouseId] uniqueidentifier NOT NULL,
    [FromBranchId] nvarchar(max) NULL,
    [ToBranchId] nvarchar(max) NULL,
    [Status] nvarchar(max) NOT NULL,
    [Remarks] nvarchar(max) NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_StockTransferHeaders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StockTransferHeaders_Warehouses_FromWarehouseId] FOREIGN KEY ([FromWarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_StockTransferHeaders_Warehouses_ToWarehouseId] FOREIGN KEY ([ToWarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [GRNHeaders] (
    [Id] uniqueidentifier NOT NULL,
    [GRNNumber] nvarchar(max) NOT NULL,
    [PurchaseOrderId] uniqueidentifier NOT NULL,
    [SupplierId] uniqueidentifier NOT NULL,
    [ReceivedDate] datetime2 NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [GatePassNo] nvarchar(max) NULL,
    [Remarks] nvarchar(max) NOT NULL,
    [IsQuick] bit NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_GRNHeaders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_GRNHeaders_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [PurchaseOrders] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Products] (
    [Id] uniqueidentifier NOT NULL,
    [CategoryId] uniqueidentifier NOT NULL,
    [SubcategoryId] uniqueidentifier NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [Sku] nvarchar(50) NOT NULL,
    [Brand] nvarchar(max) NULL,
    [Unit] nvarchar(20) NOT NULL,
    [BasePurchasePrice] decimal(18,2) NOT NULL,
    [MRP] decimal(18,2) NOT NULL,
    [Discount] decimal(18,2) NOT NULL,
    [SaleRate] decimal(18,2) NULL,
    [DefaultGst] decimal(18,2) NULL,
    [HSNCode] nvarchar(max) NOT NULL,
    [MinStock] int NOT NULL,
    [TrackInventory] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [Description] nvarchar(500) NULL,
    [ProductType] nvarchar(max) NOT NULL,
    [DamagedStock] decimal(18,2) NOT NULL,
    [IsExpiryRequired] bit NOT NULL,
    [DefaultWarehouseId] uniqueidentifier NULL,
    [DefaultRackId] uniqueidentifier NULL,
    [ImageUrl] nvarchar(max) NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Products_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Products_Racks_DefaultRackId] FOREIGN KEY ([DefaultRackId]) REFERENCES [Racks] ([Id]),
    CONSTRAINT [FK_Products_Subcategories_SubcategoryId] FOREIGN KEY ([SubcategoryId]) REFERENCES [Subcategories] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Products_Warehouses_DefaultWarehouseId] FOREIGN KEY ([DefaultWarehouseId]) REFERENCES [Warehouses] ([Id])
);
GO


CREATE TABLE [SaleOrderItems] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [SaleOrderId] uniqueidentifier NOT NULL,
    [ProductId] uniqueidentifier NOT NULL,
    [ProductName] nvarchar(max) NOT NULL,
    [Qty] decimal(18,2) NOT NULL,
    [Unit] nvarchar(max) NOT NULL,
    [Rate] decimal(18,2) NOT NULL,
    [MRP] decimal(18,2) NOT NULL,
    [DiscountAmount] decimal(18,2) NOT NULL,
    [DiscountPercent] decimal(18,2) NOT NULL,
    [GSTPercent] decimal(18,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [Total] decimal(18,2) NOT NULL,
    [MfgDate] datetime2 NULL,
    [ExpDate] datetime2 NULL,
    [WarehouseId] uniqueidentifier NULL,
    [RackId] uniqueidentifier NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_SaleOrderItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SaleOrderItems_Racks_RackId] FOREIGN KEY ([RackId]) REFERENCES [Racks] ([Id]),
    CONSTRAINT [FK_SaleOrderItems_SaleOrders_SaleOrderId] FOREIGN KEY ([SaleOrderId]) REFERENCES [SaleOrders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SaleOrderItems_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id])
);
GO


CREATE TABLE [GRNDetails] (
    [Id] uniqueidentifier NOT NULL,
    [GRNHeaderId] uniqueidentifier NOT NULL,
    [ProductId] uniqueidentifier NOT NULL,
    [OrderedQty] decimal(18,2) NOT NULL,
    [PendingQty] decimal(18,2) NOT NULL,
    [RejectedQty] decimal(18,2) NOT NULL,
    [AcceptedQty] decimal(18,2) NOT NULL,
    [ReceivedQty] decimal(18,2) NOT NULL,
    [UnitRate] decimal(18,2) NOT NULL,
    [DiscountPercent] decimal(18,2) NOT NULL,
    [GstPercent] decimal(18,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [Total] decimal(18,2) NOT NULL,
    [WarehouseId] uniqueidentifier NULL,
    [RackId] uniqueidentifier NULL,
    [MfgDate] datetime2 NULL,
    [ExpDate] datetime2 NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_GRNDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_GRNDetails_GRNHeaders_GRNHeaderId] FOREIGN KEY ([GRNHeaderId]) REFERENCES [GRNHeaders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_GRNDetails_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_GRNDetails_Racks_RackId] FOREIGN KEY ([RackId]) REFERENCES [Racks] ([Id]),
    CONSTRAINT [FK_GRNDetails_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id])
);
GO


CREATE TABLE [PriceListItems] (
    [Id] uniqueidentifier NOT NULL,
    [PriceListId] uniqueidentifier NOT NULL,
    [ProductId] uniqueidentifier NOT NULL,
    [Rate] decimal(18,2) NOT NULL,
    [Unit] nvarchar(max) NOT NULL,
    [DiscountPercent] decimal(18,2) NOT NULL,
    [MinQty] int NOT NULL,
    [MaxQty] int NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_PriceListItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PriceListItems_PriceLists_PriceListId] FOREIGN KEY ([PriceListId]) REFERENCES [PriceLists] ([Id]),
    CONSTRAINT [FK_PriceListItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [PurchaseOrderItems] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [PurchaseOrderId] uniqueidentifier NOT NULL,
    [ProductId] uniqueidentifier NOT NULL,
    [Qty] decimal(18,2) NOT NULL,
    [Unit] nvarchar(max) NOT NULL,
    [Rate] decimal(18,2) NOT NULL,
    [DiscountPercent] decimal(18,2) NOT NULL,
    [GstPercent] decimal(18,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [Total] decimal(18,2) NOT NULL,
    [ReceivedQty] decimal(18,2) NOT NULL,
    [MfgDate] datetime2 NULL,
    [ExpDate] datetime2 NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_PurchaseOrderItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PurchaseOrderItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [PurchaseOrders] ([Id])
);
GO


CREATE TABLE [SaleReturnItems] (
    [Id] uniqueidentifier NOT NULL,
    [SaleReturnHeaderId] uniqueidentifier NOT NULL,
    [ProductId] uniqueidentifier NOT NULL,
    [ReturnQty] decimal(18,2) NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [DiscountPercent] decimal(18,2) NOT NULL,
    [DiscountAmount] decimal(18,2) NOT NULL,
    [TaxPercentage] decimal(18,2) NOT NULL,
    [TaxAmount] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [Reason] nvarchar(max) NULL,
    [ItemCondition] nvarchar(max) NULL,
    [MfgDate] datetime2 NULL,
    [ExpDate] datetime2 NULL,
    [WarehouseId] uniqueidentifier NULL,
    [RackId] uniqueidentifier NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_SaleReturnItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SaleReturnItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SaleReturnItems_SaleReturnHeaders_SaleReturnHeaderId] FOREIGN KEY ([SaleReturnHeaderId]) REFERENCES [SaleReturnHeaders] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [StockTransferDetails] (
    [Id] uniqueidentifier NOT NULL,
    [StockTransferHeaderId] uniqueidentifier NOT NULL,
    [ProductId] uniqueidentifier NOT NULL,
    [Quantity] decimal(18,2) NOT NULL,
    [BatchNumber] nvarchar(max) NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_StockTransferDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StockTransferDetails_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_StockTransferDetails_StockTransferHeaders_StockTransferHeaderId] FOREIGN KEY ([StockTransferHeaderId]) REFERENCES [StockTransferHeaders] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [WarehouseStocks] (
    [Id] uniqueidentifier NOT NULL,
    [ProductId] uniqueidentifier NOT NULL,
    [WarehouseId] uniqueidentifier NOT NULL,
    [Quantity] decimal(18,2) NOT NULL,
    [MinStock] decimal(18,2) NOT NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [BranchId] nvarchar(450) NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_WarehouseStocks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_WarehouseStocks_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_WarehouseStocks_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE CASCADE
);
GO


CREATE INDEX [IX_MT_AppNotification_CompanyId] ON [AppNotifications] ([CompanyId]);
GO


CREATE INDEX [IX_Categories_ParentCategoryId] ON [Categories] ([ParentCategoryId]);
GO


CREATE INDEX [IX_MT_Category_CompanyId] ON [Categories] ([CompanyId]);
GO


CREATE INDEX [IX_MT_ExpenseCategory_CompanyId] ON [ExpenseCategories] ([CompanyId]);
GO


CREATE INDEX [IX_ExpenseEntries_CategoryId] ON [ExpenseEntries] ([CategoryId]);
GO


CREATE INDEX [IX_MT_ExpenseEntry_CompanyId] ON [ExpenseEntries] ([CompanyId]);
GO


CREATE UNIQUE INDEX [IX_GatePasses_PassNo] ON [GatePasses] ([PassNo]);
GO


CREATE INDEX [IX_MT_GatePass_CompanyId] ON [GatePasses] ([CompanyId]);
GO


CREATE INDEX [IX_GRNDetails_GRNHeaderId] ON [GRNDetails] ([GRNHeaderId]);
GO


CREATE INDEX [IX_GRNDetails_ProductId] ON [GRNDetails] ([ProductId]);
GO


CREATE INDEX [IX_GRNDetails_RackId] ON [GRNDetails] ([RackId]);
GO


CREATE INDEX [IX_GRNDetails_WarehouseId] ON [GRNDetails] ([WarehouseId]);
GO


CREATE INDEX [IX_MT_GRNDetail_CompanyId] ON [GRNDetails] ([CompanyId]);
GO


CREATE INDEX [IX_GRNHeaders_PurchaseOrderId] ON [GRNHeaders] ([PurchaseOrderId]);
GO


CREATE INDEX [IX_MT_GRNHeader_CompanyId] ON [GRNHeaders] ([CompanyId]);
GO


CREATE INDEX [IX_MT_InventoryTransaction_CompanyId] ON [InventoryTransactions] ([CompanyId]);
GO


CREATE INDEX [IX_MT_PriceListItem_CompanyId] ON [PriceListItems] ([CompanyId]);
GO


CREATE INDEX [IX_PriceListItems_PriceListId] ON [PriceListItems] ([PriceListId]);
GO


CREATE INDEX [IX_PriceListItems_ProductId] ON [PriceListItems] ([ProductId]);
GO


CREATE INDEX [IX_MT_PriceList_CompanyId] ON [PriceLists] ([CompanyId]);
GO


CREATE INDEX [IX_MT_Product_CompanyId] ON [Products] ([CompanyId]);
GO


CREATE INDEX [IX_Products_CategoryId] ON [Products] ([CategoryId]);
GO


CREATE INDEX [IX_Products_DefaultRackId] ON [Products] ([DefaultRackId]);
GO


CREATE INDEX [IX_Products_DefaultWarehouseId] ON [Products] ([DefaultWarehouseId]);
GO


CREATE UNIQUE INDEX [IX_Products_Sku] ON [Products] ([Sku]);
GO


CREATE INDEX [IX_Products_SubcategoryId] ON [Products] ([SubcategoryId]);
GO


CREATE INDEX [IX_MT_PurchaseOrderItem_CompanyId] ON [PurchaseOrderItems] ([CompanyId]);
GO


CREATE INDEX [IX_PurchaseOrderItems_ProductId] ON [PurchaseOrderItems] ([ProductId]);
GO


CREATE INDEX [IX_PurchaseOrderItems_PurchaseOrderId] ON [PurchaseOrderItems] ([PurchaseOrderId]);
GO


CREATE INDEX [IX_MT_PurchaseOrder_CompanyId] ON [PurchaseOrders] ([CompanyId]);
GO


CREATE INDEX [IX_PurchaseOrders_PriceListId] ON [PurchaseOrders] ([PriceListId]);
GO


CREATE INDEX [IX_MT_PurchaseReturnItem_CompanyId] ON [PurchaseReturnItems] ([CompanyId]);
GO


CREATE INDEX [IX_PurchaseReturnItems_PurchaseReturnId] ON [PurchaseReturnItems] ([PurchaseReturnId]);
GO


CREATE INDEX [IX_MT_PurchaseReturn_CompanyId] ON [PurchaseReturns] ([CompanyId]);
GO


CREATE INDEX [IX_MT_Rack_CompanyId] ON [Racks] ([CompanyId]);
GO


CREATE INDEX [IX_Racks_WarehouseId] ON [Racks] ([WarehouseId]);
GO


CREATE INDEX [IX_MT_SaleOrderItem_CompanyId] ON [SaleOrderItems] ([CompanyId]);
GO


CREATE INDEX [IX_SaleOrderItems_RackId] ON [SaleOrderItems] ([RackId]);
GO


CREATE INDEX [IX_SaleOrderItems_SaleOrderId] ON [SaleOrderItems] ([SaleOrderId]);
GO


CREATE INDEX [IX_SaleOrderItems_WarehouseId] ON [SaleOrderItems] ([WarehouseId]);
GO


CREATE INDEX [IX_MT_SaleOrder_CompanyId] ON [SaleOrders] ([CompanyId]);
GO


CREATE INDEX [IX_MT_SaleReturnHeader_CompanyId] ON [SaleReturnHeaders] ([CompanyId]);
GO


CREATE INDEX [IX_SaleReturnHeaders_SaleOrderId] ON [SaleReturnHeaders] ([SaleOrderId]);
GO


CREATE INDEX [IX_MT_SaleReturnItem_CompanyId] ON [SaleReturnItems] ([CompanyId]);
GO


CREATE INDEX [IX_SaleReturnItems_ProductId] ON [SaleReturnItems] ([ProductId]);
GO


CREATE INDEX [IX_SaleReturnItems_SaleReturnHeaderId] ON [SaleReturnItems] ([SaleReturnHeaderId]);
GO


CREATE INDEX [IX_MT_StockTransferDetail_CompanyId] ON [StockTransferDetails] ([CompanyId]);
GO


CREATE INDEX [IX_StockTransferDetails_ProductId] ON [StockTransferDetails] ([ProductId]);
GO


CREATE INDEX [IX_StockTransferDetails_StockTransferHeaderId] ON [StockTransferDetails] ([StockTransferHeaderId]);
GO


CREATE INDEX [IX_MT_StockTransferHeader_CompanyId] ON [StockTransferHeaders] ([CompanyId]);
GO


CREATE INDEX [IX_StockTransferHeaders_FromWarehouseId] ON [StockTransferHeaders] ([FromWarehouseId]);
GO


CREATE INDEX [IX_StockTransferHeaders_ToWarehouseId] ON [StockTransferHeaders] ([ToWarehouseId]);
GO


CREATE INDEX [IX_MT_Subcategory_CompanyId] ON [Subcategories] ([CompanyId]);
GO


CREATE INDEX [IX_Subcategories_CategoryId] ON [Subcategories] ([CategoryId]);
GO


CREATE INDEX [IX_MT_UnitMaster_CompanyId] ON [Units] ([CompanyId]);
GO


CREATE INDEX [IX_MT_Warehouse_CompanyId] ON [Warehouses] ([CompanyId]);
GO


CREATE INDEX [IX_MT_WarehouseStock_CompanyId] ON [WarehouseStocks] ([CompanyId]);
GO


CREATE INDEX [IX_WarehouseStock_Main] ON [WarehouseStocks] ([ProductId], [WarehouseId], [CompanyId], [BranchId]);
GO


CREATE INDEX [IX_WarehouseStocks_WarehouseId] ON [WarehouseStocks] ([WarehouseId]);
GO



-- 1. StockTransferHeaders Table
CREATE TABLE [StockTransferHeaders] (
    [Id] uniqueidentifier NOT NULL,
    [TransferNumber] nvarchar(50) NOT NULL,
    [TransferDate] datetime2 NOT NULL,
    [FromWarehouseId] uniqueidentifier NOT NULL,
    [ToWarehouseId] uniqueidentifier NOT NULL,
    [FromBranchId] nvarchar(max) NULL,
    [ToBranchId] nvarchar(max) NULL,
    [Status] nvarchar(50) NOT NULL DEFAULT 'Completed',
    [Remarks] nvarchar(max) NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_StockTransferHeaders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StockTransferHeaders_Warehouses_From] FOREIGN KEY ([FromWarehouseId]) REFERENCES [Warehouses] ([Id]),
    CONSTRAINT [FK_StockTransferHeaders_Warehouses_To] FOREIGN KEY ([ToWarehouseId]) REFERENCES [Warehouses] ([Id])
);
GO

-- 2. StockTransferDetails Table
CREATE TABLE [StockTransferDetails] (
    [Id] uniqueidentifier NOT NULL,
    [StockTransferHeaderId] uniqueidentifier NOT NULL,
    [ProductId] uniqueidentifier NOT NULL,
    [Quantity] decimal(18, 2) NOT NULL,
    [BatchNumber] nvarchar(100) NULL,
    [CompanyId] uniqueidentifier NOT NULL,
    [BranchId] nvarchar(max) NULL,
    [CreatedOn] datetime2 NULL,
    [CreatedBy] nvarchar(max) NULL,
    [ModifiedOn] datetime2 NULL,
    [ModifiedBy] nvarchar(max) NULL,
    CONSTRAINT [PK_StockTransferDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_StockTransferDetails_Headers] FOREIGN KEY ([StockTransferHeaderId]) REFERENCES [StockTransferHeaders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_StockTransferDetails_Products] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id])
);
GO

CREATE INDEX [IX_StockTransferDetails_HeaderId] ON [StockTransferDetails] ([StockTransferHeaderId]);
GO
