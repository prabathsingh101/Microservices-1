-- =============================================
-- Migration: Add Extended Profile Fields to Users Table
-- Database: IdentityDb
-- Run this on your local database instance
-- =============================================

USE [IdentityDb];
GO

-- 1. FirstName
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'FirstName')
BEGIN
    ALTER TABLE [Users] ADD [FirstName] NVARCHAR(100) NULL;
    PRINT 'Added FirstName column.';
END;

-- 2. LastName
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'LastName')
BEGIN
    ALTER TABLE [Users] ADD [LastName] NVARCHAR(100) NULL;
    PRINT 'Added LastName column.';
END;

-- 3. PhoneNumber
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'PhoneNumber')
BEGIN
    ALTER TABLE [Users] ADD [PhoneNumber] NVARCHAR(20) NULL;
    PRINT 'Added PhoneNumber column.';
END;

-- 4. Designation
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Designation')
BEGIN
    ALTER TABLE [Users] ADD [Designation] NVARCHAR(100) NULL;
    PRINT 'Added Designation column.';
END;

-- 5. Department
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Department')
BEGIN
    ALTER TABLE [Users] ADD [Department] NVARCHAR(100) NULL;
    PRINT 'Added Department column.';
END;

-- 6. Address
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Address')
BEGIN
    ALTER TABLE [Users] ADD [Address] NVARCHAR(500) NULL;
    PRINT 'Added Address column.';
END;

-- 7. City
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'City')
BEGIN
    ALTER TABLE [Users] ADD [City] NVARCHAR(100) NULL;
    PRINT 'Added City column.';
END;

-- 8. State
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'State')
BEGIN
    ALTER TABLE [Users] ADD [State] NVARCHAR(100) NULL;
    PRINT 'Added State column.';
END;

-- 9. Pincode
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Pincode')
BEGIN
    ALTER TABLE [Users] ADD [Pincode] NVARCHAR(20) NULL;
    PRINT 'Added Pincode column.';
END;

-- 10. Gender
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Gender')
BEGIN
    ALTER TABLE [Users] ADD [Gender] NVARCHAR(20) NULL;
    PRINT 'Added Gender column.';
END;

-- 11. DateOfBirth
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'DateOfBirth')
BEGIN
    ALTER TABLE [Users] ADD [DateOfBirth] DATETIME2 NULL;
    PRINT 'Added DateOfBirth column.';
END;

-- 12. AadhaarUrl
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'AadhaarUrl')
BEGIN
    ALTER TABLE [Users] ADD [AadhaarUrl] NVARCHAR(MAX) NULL;
    PRINT 'Added AadhaarUrl column.';
END;

-- 13. PanCardUrl
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'PanCardUrl')
BEGIN
    ALTER TABLE [Users] ADD [PanCardUrl] NVARCHAR(MAX) NULL;
    PRINT 'Added PanCardUrl column.';
END;

PRINT 'Extended user profile fields migration complete!';
GO
