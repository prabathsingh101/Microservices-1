-- SQL Script to add PaymentStatus column to Subscriptions table

ALTER TABLE Subscriptions
ADD PaymentStatus NVARCHAR(50) DEFAULT 'Pending' NOT NULL;
GO

-- If you have existing records, you might want to update them:
UPDATE Subscriptions SET PaymentStatus = 'Success' WHERE PaymentTxnId IS NOT NULL;
UPDATE Subscriptions SET PaymentStatus = 'Trial' WHERE PlanType = 'Trial';
GO
