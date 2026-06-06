USE InventoryDb;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SalesInvoiceDeliveryChallans]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SalesInvoiceDeliveryChallans] (
        [SalesInvoiceId] UNIQUEIDENTIFIER NOT NULL,
        [DeliveryChallanId] UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT [PK_SalesInvoiceDeliveryChallans] PRIMARY KEY CLUSTERED ([SalesInvoiceId] ASC, [DeliveryChallanId] ASC),
        CONSTRAINT [FK_SalesInvoiceDeliveryChallans_SalesInvoices] FOREIGN KEY ([SalesInvoiceId]) REFERENCES [dbo].[SalesInvoices] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_SalesInvoiceDeliveryChallans_DeliveryChallans] FOREIGN KEY ([DeliveryChallanId]) REFERENCES [dbo].[DeliveryChallans] ([Id])
    );
    
    PRINT 'SalesInvoiceDeliveryChallans relation table created successfully.';
END
GO
