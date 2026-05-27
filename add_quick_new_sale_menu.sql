USE IdentityDb;
GO

DECLARE @ParentId UNIQUEIDENTIFIER;
DECLARE @Order INT;

-- Get the ParentId and Order from the existing "Quick Sale" menu
SELECT @ParentId = ParentId, @Order = [Order] + 1
FROM Menus 
WHERE Title = 'Quick Sale' AND [Url] LIKE '%/app/quick-inventory/sale%';

IF @ParentId IS NOT NULL
BEGIN
    -- Shift the order of subsequent menus down to make space for the new menu
    UPDATE Menus
    SET [Order] = [Order] + 1
    WHERE ParentId = @ParentId AND [Order] >= @Order;

    -- Insert Menu Item
    IF NOT EXISTS (SELECT 1 FROM Menus WHERE [Url] = '/app/quick-inventory/sale/new-list')
    BEGIN
        INSERT INTO Menus (Id, Title, [Url], Icon, ParentId, [Order], CreatedDate)
        VALUES (NEWID(), 'Quick New Sale', '/app/quick-inventory/sale/new-list', 'shopping_basket', @ParentId, @Order, GETDATE());
        
        PRINT 'Menu "Quick New Sale" added successfully.';
    END
    ELSE
    BEGIN
        PRINT 'Menu "Quick New Sale" already exists.';
    END

    -- Assign Permissions to Super Admin
    DECLARE @MenuId UNIQUEIDENTIFIER;
    SELECT @MenuId = Id FROM Menus WHERE [Url] = '/app/quick-inventory/sale/new-list';

    DECLARE @SuperAdminRoleId UNIQUEIDENTIFIER;
    SELECT @SuperAdminRoleId = Id FROM Roles WHERE RoleName = 'Super Admin' OR RoleName = 'Default Admin';

    IF @MenuId IS NOT NULL AND @SuperAdminRoleId IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM RolePermissions WHERE MenuId = @MenuId AND RoleId = @SuperAdminRoleId)
        BEGIN
            INSERT INTO RolePermissions (Id, RoleId, MenuId, CanView, CanAdd, CanEdit, CanDelete, CreatedDate)
            VALUES (NEWID(), @SuperAdminRoleId, @MenuId, 1, 1, 1, 1, GETDATE());
            
            PRINT 'Permissions assigned to Super Admin successfully.';
        END
    END
END
ELSE
BEGIN
    PRINT 'Error: Could not find the "Quick Sale" menu to determine the parent.';
END
GO
