USE IdentityDb;
GO

-- 1. Admin Parent ID dhundte hain
DECLARE @AdminParentId UNIQUEIDENTIFIER;
SELECT TOP 1 @AdminParentId = Id FROM Menus WHERE Title = 'Admin' AND ParentId IS NULL;

-- 2. "Role Management" menu create karte hain (Table view ke liye)
IF NOT EXISTS (SELECT 1 FROM Menus WHERE Url = '/app/admin/roles')
BEGIN
    INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order]) 
    VALUES (NEWID(), 'Role Management', '/app/admin/roles', 'admin_panel_settings', @AdminParentId, 10);
END

-- 3. "Role Permissions" menu create karte hain (Mapping ke liye)
IF NOT EXISTS (SELECT 1 FROM Menus WHERE Url = '/app/admin/role-permissions')
BEGIN
    INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order]) 
    VALUES (NEWID(), 'Role Permissions', '/app/admin/role-permissions', 'security', @AdminParentId, 11);
END

-- 4. Purane "Users & Roles" ka naam "User Management" kar dete hain confusion hatane ke liye
UPDATE Menus SET Title = 'User Management' WHERE Title = 'Users & Roles';

-- 5. Admin role ko in sabka access automatically mil jaye
INSERT INTO RolePermissions (Id, RoleId, MenuId, CanView, CanAdd, CanEdit, CanDelete, CompanyId, AdditionalActions)
SELECT NEWID(), '00000000-0000-0000-0000-000000000001', m.Id, 1, 1, 1, 1, NULL, 'ALL'
FROM Menus m
LEFT JOIN RolePermissions rp ON rp.MenuId = m.Id AND rp.RoleId = '00000000-0000-0000-0000-000000000001'
WHERE m.Url IN ('/app/admin/roles', '/app/admin/role-permissions') AND rp.Id IS NULL;

GO
PRINT 'Role Management menus added and permissions granted to Admin.';
