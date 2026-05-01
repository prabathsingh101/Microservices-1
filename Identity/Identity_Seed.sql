SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
USE IdentityDb;
GO

-- 1. CLEANUP
DELETE FROM RolePermissions;
DELETE FROM UserRoles;
DELETE FROM Menus;
DELETE FROM Roles;
DELETE FROM Users;
GO

-- 2. CREATE ADMIN ROLE
INSERT INTO Roles (Id, RoleName, CompanyId, CreatedDate)
VALUES ('00000000-0000-0000-0000-000000000001', 'Admin', NULL, GETDATE());

-- 3. CREATE DEFAULT ADMIN USER
DECLARE @UserId UNIQUEIDENTIFIER = NEWID();
INSERT INTO Users (Id, UserName, Email, PasswordHash, IsActive, CompanyId, CreatedDate)
VALUES (
    @UserId, 
    'admin', 
    'Default_Admin@gmail.com', 
    'AQAAAAIAAYagAAAAELeEMOEeHADE/M69ks/z3Xo+fN6tWf1gQ6kC4t9H1N0j+5dF0X6+Wl7j/Yv7F1j9Xw==', 
    1, 
    NULL,
    GETDATE()
);

-- 4. LINK USER TO ROLE
INSERT INTO UserRoles (Id, UserId, RoleId, CompanyId, CreatedDate)
VALUES (NEWID(), @UserId, '00000000-0000-0000-0000-000000000001', NULL, GETDATE());

-- 5. CREATE MENUS (54 Total)
DECLARE @P_Admin UNIQUEIDENTIFIER = NEWID();
DECLARE @P_Core UNIQUEIDENTIFIER = NEWID();
DECLARE @P_HR UNIQUEIDENTIFIER = NEWID();
DECLARE @P_Finance UNIQUEIDENTIFIER = NEWID();
DECLARE @P_Inventory UNIQUEIDENTIFIER = NEWID();
DECLARE @P_Production UNIQUEIDENTIFIER = NEWID();
DECLARE @P_Sale UNIQUEIDENTIFIER = NEWID();
DECLARE @P_Reports UNIQUEIDENTIFIER = NEWID();
DECLARE @P_Purchase UNIQUEIDENTIFIER = NEWID();

-- Parents
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (@P_Admin, 'Admin', '#', 'settings', NULL, 1, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (@P_Core, 'Core', '#', 'apps', NULL, 2, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (@P_HR, 'HR', '#', 'people', NULL, 3, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (@P_Finance, 'Finance', '#', 'account_balance', NULL, 4, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (@P_Inventory, 'Inventory', '#', 'inventory_2', NULL, 5, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (@P_Production, 'Production', '#', 'precision_manufacturing', NULL, 6, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (@P_Sale, 'Sale', '#', 'shopping_cart', NULL, 7, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (@P_Purchase, 'Purchase', '#', 'shopping_bag', NULL, 8, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (@P_Reports, 'Reports', '#', 'bar_chart', NULL, 9, GETDATE());

-- Admin Sub-menus
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Company Profile', '/app/admin/company', 'business', @P_Admin, 1, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Users & Roles', '/app/admin/users', 'group', @P_Admin, 2, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Backup & Restore', '/app/admin/backup', 'backup', @P_Admin, 3, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Print Settings', '/app/admin/print-settings', 'print', @P_Admin, 4, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'App Settings', '/app/admin/settings', 'settings', @P_Admin, 5, GETDATE());

-- Core Sub-menus
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Customers', '/app/core/customers', 'person_pin', @P_Core, 1, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Suppliers', '/app/core/suppliers', 'local_shipping', @P_Core, 2, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Products', '/app/core/products', 'category', @P_Core, 3, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Taxes', '/app/core/taxes', 'receipt', @P_Core, 4, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Units', '/app/core/units', 'straighten', @P_Core, 5, GETDATE());

-- HR Sub-menus
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Employee Groups', '/app/hr/employee-groups', 'groups', @P_HR, 1, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Designations', '/app/hr/designations', 'badge', @P_HR, 2, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Staff List', '/app/hr/employees', 'list_alt', @P_HR, 3, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Attendance', '/app/hr/attendance', 'event_available', @P_HR, 4, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Payroll', '/app/hr/payroll', 'payments', @P_HR, 5, GETDATE());

-- Finance Sub-menus
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Account Groups', '/app/finance/groups', 'folder', @P_Finance, 1, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Ledgers', '/app/finance/ledgers', 'menu_book', @P_Finance, 2, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Payment Voucher', '/app/finance/vouchers/payment', 'payment', @P_Finance, 3, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Receipt Voucher', '/app/finance/vouchers/receipt', 'receipt_long', @P_Finance, 4, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Journal Voucher', '/app/finance/vouchers/journal', 'draw', @P_Finance, 5, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Expense Entry', '/app/finance/expenses', 'money_off', @P_Finance, 6, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Bank Reconciliation', '/app/finance/bank-recon', 'account_balance_wallet', @P_Finance, 7, GETDATE());

-- Inventory Sub-menus
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Item Groups', '/app/inventory/groups', 'inventory', @P_Inventory, 1, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Stock Adjustments', '/app/inventory/adjustments', 'published_with_changes', @P_Inventory, 2, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Physical Stock Count', '/app/inventory/stock-count', 'check_circle', @P_Inventory, 3, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Item Transfer', '/app/inventory/transfers', 'move_up', @P_Inventory, 4, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Opening Stock', '/app/inventory/opening-stock', 'first_page', @P_Inventory, 5, GETDATE());

-- Production Sub-menus
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Bill of Materials (BOM)', '/app/production/bom', 'receipt_long', @P_Production, 1, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Work Order', '/app/production/work-order', 'assignment', @P_Production, 2, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Production Entry', '/app/production/entry', 'factory', @P_Production, 3, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Job Work', '/app/production/job-work', 'engineering', @P_Production, 4, GETDATE());

-- Sale Sub-menus
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Quotation', '/app/sale/quote', 'format_quote', @P_Sale, 1, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Sale Order', '/app/sale/order', 'shopping_basket', @P_Sale, 2, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Sale Invoice', '/app/sale/invoice', 'point_of_sale', @P_Sale, 3, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Sale Return', '/app/sale/return', 'assignment_return', @P_Sale, 4, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Quick Sale (POS)', '/app/sale/pos', 'bolt', @P_Sale, 5, GETDATE());

-- Purchase Sub-menus
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Purchase Order', '/app/purchase/order', 'add_shopping_cart', @P_Purchase, 1, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Purchase Invoice', '/app/purchase/invoice', 'shopping_cart_checkout', @P_Purchase, 2, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Purchase Return', '/app/purchase/return', 'keyboard_return', @P_Purchase, 3, GETDATE());

-- Reports Sub-menus
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Stock Report', '/app/reports/inventory/stock', 'analytics', @P_Reports, 1, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Sales Report', '/app/reports/sales/summary', 'trending_up', @P_Reports, 2, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Purchase Report', '/app/reports/purchase/summary', 'trending_down', @P_Reports, 3, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'P&L Statement', '/app/reports/finance/profit-loss', 'monetization_on', @P_Reports, 4, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Balance Sheet', '/app/reports/finance/balance-sheet', 'account_balance', @P_Reports, 5, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'GST Reports', '/app/reports/tax/gst', 'gavel', @P_Reports, 6, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Customer Ledger', '/app/reports/customers/ledger', 'description', @P_Reports, 7, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Supplier Ledger', '/app/reports/suppliers/ledger', 'description', @P_Reports, 8, GETDATE());
INSERT INTO Menus (Id, Title, Url, Icon, ParentId, [Order], CreatedDate) VALUES (NEWID(), 'Item Ledger', '/app/reports/inventory/item-ledger', 'description', @P_Reports, 9, GETDATE());

-- 6. GRANT ALL PERMISSIONS TO ADMIN ROLE
INSERT INTO RolePermissions (Id, RoleId, MenuId, CanView, CanAdd, CanEdit, CanDelete, CompanyId, AdditionalActions, CreatedDate)
SELECT NEWID(), '00000000-0000-0000-0000-000000000001', Id, 1, 1, 1, 1, NULL, 'PRINT,APPROVE,REJECT,EXPORT', GETDATE()
FROM Menus;

PRINT 'Seeding Completed with 54 Menus and Admin Access.';
GO
