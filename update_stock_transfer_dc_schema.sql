-- SQL Schema Migration Script
-- Target Database: InventoryDb
-- Description: Add transport metadata columns to StockTransferHeaders and link it to DeliveryChallans

BEGIN TRANSACTION;

-- 1. Add transport details columns to StockTransferHeaders
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[StockTransferHeaders]') AND name = N'VehicleRegNo')
BEGIN
    ALTER TABLE [dbo].[StockTransferHeaders]
    ADD [VehicleRegNo] NVARCHAR(50) NULL,
        [TransporterName] NVARCHAR(100) NULL,
        [DriverName] NVARCHAR(100) NULL,
        [EWayBillNo] NVARCHAR(100) NULL;
    
    PRINT 'Added Transport Columns to StockTransferHeaders table successfully.';
END
ELSE
BEGIN
    PRINT 'Transport Columns already exist in StockTransferHeaders table.';
END

-- 2. Add StockTransferHeaderId column and Foreign Key constraint to DeliveryChallans
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[DeliveryChallans]') AND name = N'StockTransferHeaderId')
BEGIN
    ALTER TABLE [dbo].[DeliveryChallans]
    ADD [StockTransferHeaderId] UNIQUEIDENTIFIER NULL;

    -- Add Foreign Key Constraint
    ALTER TABLE [dbo].[DeliveryChallans] WITH CHECK 
    ADD CONSTRAINT [FK_DeliveryChallans_StockTransferHeaders] 
    FOREIGN KEY ([StockTransferHeaderId]) REFERENCES [dbo].[StockTransferHeaders] ([Id]);

    ALTER TABLE [dbo].[DeliveryChallans] CHECK CONSTRAINT [FK_DeliveryChallans_StockTransferHeaders];

    PRINT 'Added StockTransferHeaderId and Foreign Key constraint to DeliveryChallans table successfully.';
END
ELSE
BEGIN
    PRINT 'StockTransferHeaderId already exists in DeliveryChallans table.';
END

COMMIT TRANSACTION;
