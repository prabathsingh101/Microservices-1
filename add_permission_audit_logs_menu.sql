USE IdentityDb;
GO

-- 1. Identify Admin parent menu
DECLARE @ParentId UNIQUEIDENTIFIER = NULL;
SELECT @ParentId = Id FROM Menus WHERE [Url] LIKE '%admin%' AND ParentId IS NULL;

IF @ParentId IS NULL
BEGIN
    SELECT @ParentId = Id FROM Menus WHERE Title = 'Admin' OR Title = 'Settings';
END

-- 2. Insert Menu Item
IF NOT EXISTS (SELECT 1 FROM Menus WHERE [Url] = '/app/admin/permission-audit-logs')
BEGIN
    INSERT INTO Menus (Id, Title, [Url], Icon, ParentId, [Order], CreatedDate)
    VALUES (NEWID(), 'Permission Audit Logs', '/app/admin/permission-audit-logs', 'rule', @ParentId, 101, GETDATE());
    
    PRINT 'Menu item added successfully.';
END

-- 3. Assign Permissions to Super Admin
DECLARE @MenuId UNIQUEIDENTIFIER;
SELECT @MenuId = Id FROM Menus WHERE [Url] = '/app/admin/permission-audit-logs';

DECLARE @SuperAdminRoleId UNIQUEIDENTIFIER;
-- Search for Super Admin or Default Admin
SELECT @SuperAdminRoleId = Id FROM Roles WHERE RoleName = 'Super Admin' OR RoleName = 'Default Admin';

IF @MenuId IS NOT NULL AND @SuperAdminRoleId IS NOT NULL
BEGIN
    DELETE FROM RolePermissions WHERE MenuId = @MenuId AND RoleId = @SuperAdminRoleId;

    INSERT INTO RolePermissions (Id, RoleId, MenuId, CanView, CanAdd, CanEdit, CanDelete, CreatedDate)
    VALUES (NEWID(), @SuperAdminRoleId, @MenuId, 1, 1, 1, 1, GETDATE());
    
    PRINT 'Permissions assigned to Super Admin successfully.';
END
GO
