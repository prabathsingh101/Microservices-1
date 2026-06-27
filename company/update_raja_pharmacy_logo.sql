USE CompanyDb;
GO

-- Update Raja Pharmacy logo
UPDATE CompanyProfiles
SET LogoUrl = '/uploads/logos/raja_pharmacy_logo.png'
WHERE Name LIKE '%Raja Pharmacy%';
GO

PRINT 'Raja Pharmacy logo URL updated successfully.';
GO
