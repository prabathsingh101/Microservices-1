USE IdentityDb;
GO

-- 1. Identify Admin parent menu
DECLARE @ParentId INT = NULL;
SELECT @ParentId = Id FROM Menus WHERE [Url] LIKE '%admin%' AND ParentId IS NULL;

IF @ParentId IS NULL
BEGIN
    SELECT @ParentId = Id FROM Menus WHERE Title = 'Admin' OR Title = 'Settings';
END

-- 2. Insert Menu Item (without IsActive)
IF NOT EXISTS (SELECT 1 FROM Menus WHERE [Url] = '/app/admin/system-logs')
BEGIN
    INSERT INTO Menus (Title, [Url], Icon, ParentId, [Order])
    VALUES ('System Logs', '/app/admin/system-logs', 'history_edu', @ParentId, 100);
    
    PRINT 'Menu item added successfully.';
END

-- 3. Assign Permissions
DECLARE @MenuId INT;
SELECT @MenuId = Id FROM Menus WHERE [Url] = '/app/admin/system-logs';

DECLARE @AdminRoleId INT;
SELECT @AdminRoleId = Id FROM Roles WHERE RoleName = 'Admin' OR RoleName = 'SuperAdmin';

IF @MenuId IS NOT NULL AND @AdminRoleId IS NOT NULL
BEGIN
    DELETE FROM RolePermissions WHERE MenuId = @MenuId AND RoleId = @AdminRoleId;

    INSERT INTO RolePermissions (RoleId, MenuId, CanView, CanAdd, CanEdit, CanDelete)
    VALUES (@AdminRoleId, @MenuId, 1, 0, 0, 1);
    
    PRINT 'Permissions assigned successfully.';
END
GO
