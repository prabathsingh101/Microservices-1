-- =============================================
-- Google Social Login: DB Schema Changes
-- Run this on IdentityDb
-- Date: 2026-05-19
-- =============================================

-- Step 1: Make PasswordHash nullable (Social login users won't have password)
-- Only run if column is currently NOT NULL
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Users'
      AND COLUMN_NAME = 'PasswordHash'
      AND IS_NULLABLE = 'NO'
)
BEGIN
    ALTER TABLE [Users] ALTER COLUMN [PasswordHash] NVARCHAR(MAX) NULL;
    PRINT 'PasswordHash column made nullable.';
END
ELSE
BEGIN
    PRINT 'PasswordHash is already nullable. Skipping.';
END

-- Step 2: Add AuthProvider column (default = 'local')
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'AuthProvider'
)
BEGIN
    ALTER TABLE [Users] ADD [AuthProvider] NVARCHAR(20) NOT NULL DEFAULT 'local';
    PRINT 'AuthProvider column added.';
END
ELSE
BEGIN
    PRINT 'AuthProvider column already exists. Skipping.';
END

-- Step 3: Add GoogleId column
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'GoogleId'
)
BEGIN
    ALTER TABLE [Users] ADD [GoogleId] NVARCHAR(100) NULL;
    PRINT 'GoogleId column added.';
END
ELSE
BEGIN
    PRINT 'GoogleId column already exists. Skipping.';
END

-- Step 4: Verify
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users'
  AND COLUMN_NAME IN ('PasswordHash', 'AuthProvider', 'GoogleId')
ORDER BY COLUMN_NAME;

PRINT 'Social Login DB migration complete!';
