CREATE TABLE [CompanyProfiles] (
    [Id] uniqueidentifier NOT NULL,
    [CompanyCode] nvarchar(50) NULL,
    [Name] nvarchar(max) NULL,
    [Tagline] nvarchar(max) NULL,
    [RegistrationNumber] nvarchar(max) NULL,
    [Gstin] nvarchar(15) NOT NULL,
    [LogoUrl] nvarchar(max) NULL,
    [PrimaryEmail] nvarchar(max) NULL,
    [Email] nvarchar(max) NULL,
    [SmtpEmail] nvarchar(max) NULL,
    [SmtpPassword] nvarchar(max) NULL,
    [SmtpHost] nvarchar(max) NULL,
    [SmtpPort] int NULL,
    [SmtpUseSsl] bit NOT NULL,
    [PrimaryPhone] nvarchar(max) NULL,
    [Website] nvarchar(max) NULL,
    [Message] nvarchar(max) NULL,
    [DriverWhatsAppMessage] nvarchar(max) NULL,
    [SaleReturnWindowValue] int NOT NULL,
    [SaleReturnWindowUnit] nvarchar(max) NULL,
    [SaleReturnPolicyDisclaimer] nvarchar(max) NULL,
    [PurchaseReturnWindowValue] int NOT NULL,
    [PurchaseReturnWindowUnit] nvarchar(max) NOT NULL,
    [PurchaseReturnPolicyDisclaimer] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [InvoiceFooterMessage] nvarchar(max) NULL,
    [EstimateFooterMessage] nvarchar(max) NULL,
    [PurchaseOrderFooterMessage] nvarchar(max) NULL,
    [SaleOrderFooterMessage] nvarchar(max) NULL,
    [PurchaseOrderCreationMessage] nvarchar(max) NULL,
    [PurchaseOrderStatusUpdateMessage] nvarchar(max) NULL,
    [SaleOrderCreationMessage] nvarchar(max) NULL,
    [SaleOrderConfirmationMessage] nvarchar(max) NULL,
    CONSTRAINT [PK_CompanyProfiles] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Addresses] (
    [Id] int NOT NULL IDENTITY,
    [CompanyProfileId] uniqueidentifier NULL,
    [BranchName] nvarchar(max) NULL,
    [AddressLine1] nvarchar(max) NULL,
    [AddressLine2] nvarchar(max) NULL,
    [City] nvarchar(max) NULL,
    [State] nvarchar(max) NULL,
    [StateCode] nvarchar(2) NULL,
    [PinCode] nvarchar(max) NULL,
    [Country] nvarchar(max) NULL,
    [Email] nvarchar(max) NULL,
    [Phone] nvarchar(max) NULL,
    [ContactPerson] nvarchar(max) NULL,
    [Gstin] nvarchar(max) NULL,
    [IsHeadOffice] bit NOT NULL,
    CONSTRAINT [PK_Addresses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Addresses_CompanyProfiles_CompanyProfileId] FOREIGN KEY ([CompanyProfileId]) REFERENCES [CompanyProfiles] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [AuthorizedSignatories] (
    [Id] int NOT NULL IDENTITY,
    [CompanyProfileId] uniqueidentifier NULL,
    [PersonName] nvarchar(max) NULL,
    [Designation] nvarchar(max) NULL,
    [SignatureImageUrl] nvarchar(max) NULL,
    [Email] nvarchar(max) NULL,
    [IsDefault] bit NOT NULL,
    CONSTRAINT [PK_AuthorizedSignatories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AuthorizedSignatories_CompanyProfiles_CompanyProfileId] FOREIGN KEY ([CompanyProfileId]) REFERENCES [CompanyProfiles] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [BankDetails] (
    [Id] int NOT NULL IDENTITY,
    [CompanyProfileId] uniqueidentifier NULL,
    [BankName] nvarchar(max) NULL,
    [BranchName] nvarchar(max) NULL,
    [AccountNumber] nvarchar(max) NULL,
    [IfscCode] nvarchar(max) NULL,
    [AccountType] nvarchar(max) NULL,
    [Email] nvarchar(max) NULL,
    CONSTRAINT [PK_BankDetails] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BankDetails_CompanyProfiles_CompanyProfileId] FOREIGN KEY ([CompanyProfileId]) REFERENCES [CompanyProfiles] ([Id]) ON DELETE CASCADE
);
GO


CREATE INDEX [IX_Addresses_CompanyProfileId] ON [Addresses] ([CompanyProfileId]);
GO


CREATE INDEX [IX_AuthorizedSignatories_CompanyProfileId] ON [AuthorizedSignatories] ([CompanyProfileId]);
GO


CREATE INDEX [IX_BankDetails_CompanyProfileId] ON [BankDetails] ([CompanyProfileId]);
GO


CREATE UNIQUE INDEX [IX_CompanyProfiles_CompanyCode] ON [CompanyProfiles] ([CompanyCode]) WHERE [CompanyCode] IS NOT NULL;
GO


