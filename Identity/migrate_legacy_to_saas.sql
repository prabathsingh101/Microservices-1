/* 
   --------------------------------------------------------------------------
   MIGRATION SCRIPT: LEGACY TO MULTI-TENANT (KRISHNA MART)
   --------------------------------------------------------------------------
   Use this script to assign a unique CompanyId to all existing data 
   associated with your original "Krishna Mart" setup.
*/

-- 1. Define the Fixed Company ID for Krishna Mart
DECLARE @KrishnaMartId UNIQUEIDENTIFIER = '88888888-4444-4444-4444-000000000001';

-- ==========================================
-- IDENTITY DATABASE UPDATES
-- ==========================================

-- A. Create Subscription for Krishna Mart (if doesn't exist)
IF NOT EXISTS (SELECT 1 FROM Subscriptions WHERE CompanyId = @KrishnaMartId)
BEGIN
    INSERT INTO Subscriptions (CompanyId, CompanyName, PlanType, StartDate, EndDate, IsActive)
    VALUES (@KrishnaMartId, 'Krishna Mart', 'Lifetime', GETUTCDATE(), '2099-12-31', 1);
END

-- B. Update existing Roles (Assign to Krishna Mart)
UPDATE Roles SET CompanyId = @KrishnaMartId WHERE CompanyId IS NULL;

-- C. Update existing RolePermissions
UPDATE RolePermissions SET CompanyId = @KrishnaMartId WHERE CompanyId IS NULL;

-- D. Update existing Users (Assign to Krishna Mart)
-- Use this for your admin@admin.com or other old users
UPDATE Users SET CompanyId = @KrishnaMartId WHERE CompanyId IS NULL;


-- ==========================================
-- INVENTORY / BUSINESS DATABASE UPDATES
-- ==========================================
-- Note: Run these in your Inventory Database context

-- UPDATE Categories SET CompanyId = @KrishnaMartId WHERE CompanyId IS NULL;
-- UPDATE Products SET CompanyId = @KrishnaMartId WHERE CompanyId IS NULL;
-- UPDATE Stocks SET CompanyId = @KrishnaMartId WHERE CompanyId IS NULL;

PRINT 'Migration to "Krishna Mart" tenant completed successfully!';
