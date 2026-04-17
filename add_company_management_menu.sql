-- add_company_management_menu.sql
USE IdentityDb;
GO

DECLARE @AdminParentId UNIQUEIDENTIFIER;
-- Find the "Admin" root menu
SELECT TOP 1 @AdminParentId = Id FROM Menus WHERE Title = 'Admin' AND ParentId IS NULL;

-- If not found (maybe translated), check for common titles
IF @AdminParentId IS NULL
    SELECT TOP 1 @AdminParentId = Id FROM Menus WHERE Title LIKE '%Admin%' AND ParentId IS NULL;

-- 1. Add "Company Management" menu child
IF NOT EXISTS (SELECT 1 FROM Menus WHERE Url = '/app/admin/companies')
BEGIN
    INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], IsActive) 
    VALUES (NEWID(), 'Company Management', '/app/admin/companies', 'storefront', @AdminParentId, 12, 1);
END

-- 2. Find the Super Admin Role (CompanyId IS NULL in Roles)
DECLARE @AdminRoleId UNIQUEIDENTIFIER;
SELECT TOP 1 @AdminRoleId = Id FROM Roles WHERE Name = 'Admin' AND CompanyId IS NULL;

-- 3. Grant Permissions for this new menu to Super Admin
IF @AdminRoleId IS NOT NULL
BEGIN
    INSERT INTO RolePermissions (Id, RoleId, MenuId, CanView, CanAdd, CanEdit, CanDelete, CompanyId, AdditionalActions)
    SELECT NEWID(), @AdminRoleId, m.Id, 1, 1, 1, 1, NULL, 'ALL'
    FROM Menus m
    WHERE m.Url = '/app/admin/companies'
    AND NOT EXISTS (SELECT 1 FROM RolePermissions WHERE RoleId = @AdminRoleId AND MenuId = m.Id);
END
GO
