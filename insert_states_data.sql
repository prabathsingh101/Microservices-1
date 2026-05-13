-- Create States Table if not exists
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[States]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[States] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [Name] NVARCHAR(100) NOT NULL,
        [Code] NVARCHAR(10) NOT NULL,
        [DefaultCity] NVARCHAR(100) NOT NULL,
        [DefaultPinCode] NVARCHAR(10) NOT NULL,
        CONSTRAINT [PK_States] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

-- Since States is a static master lookup table, reload it clean
TRUNCATE TABLE [dbo].[States];
GO

SET IDENTITY_INSERT [dbo].[States] ON;

INSERT INTO [dbo].[States] ([Id], [Name], [Code], [DefaultCity], [DefaultPinCode]) VALUES
(1, N'Andaman and Nicobar Islands', N'35', N'Port Blair', N'744101'),
(2, N'Andhra Pradesh', N'37', N'Vijayawada', N'520001'),
(3, N'Arunachal Pradesh', N'12', N'Itanagar', N'791111'),
(4, N'Assam', N'18', N'Guwahati', N'781001'),
(5, N'Bihar', N'10', N'Patna', N'800001'),
(6, N'Chandigarh', N'04', N'Chandigarh', N'160017'),
(7, N'Chhattisgarh', N'22', N'Raipur', N'492001'),
(8, N'Dadra and Nagar Haveli', N'26', N'Silvassa', N'396230'),
(9, N'Daman and Diu', N'26', N'Daman', N'396210'),
(10, N'Delhi', N'07', N'New Delhi', N'110001'),
(11, N'Goa', N'30', N'Panaji', N'403001'),
(12, N'Gujarat', N'24', N'Ahmedabad', N'380001'),
(13, N'Haryana', N'06', N'Gurugram', N'122001'),
(14, N'Himachal Pradesh', N'02', N'Shimla', N'171001'),
(15, N'Jammu and Kashmir', N'01', N'Srinagar', N'190001'),
(16, N'Jharkhand', N'20', N'Ranchi', N'834001'),
(17, N'Karnataka', N'29', N'Bengaluru', N'560001'),
(18, N'Kerala', N'32', N'Thiruvananthapuram', N'695001'),
(19, N'Ladakh', N'38', N'Leh', N'194101'),
(20, N'Lakshadweep', N'31', N'Kavaratti', N'682555'),
(21, N'Madhya Pradesh', N'23', N'Bhopal', N'462001'),
(22, N'Maharashtra', N'27', N'Mumbai', N'400001'),
(23, N'Manipur', N'14', N'Imphal', N'795001'),
(24, N'Meghalaya', N'17', N'Shillong', N'793001'),
(25, N'Mizoram', N'15', N'Aizawl', N'796001'),
(26, N'Nagaland', N'13', N'Kohima', N'797001'),
(27, N'Odisha', N'21', N'Bhubaneswar', N'751001'),
(28, N'Puducherry', N'34', N'Puducherry', N'605001'),
(29, N'Punjab', N'03', N'Amritsar', N'143001'),
(30, N'Rajasthan', N'08', N'Jaipur', N'302001'),
(31, N'Sikkim', N'11', N'Gangtok', N'737101'),
(32, N'Tamil Nadu', N'33', N'Chennai', N'600001'),
(33, N'Telangana', N'36', N'Hyderabad', N'500001'),
(34, N'Tripura', N'16', N'Agartala', N'799001'),
(35, N'Uttar Pradesh', N'09', N'Lucknow', N'226001'),
(36, N'Uttarakhand', N'05', N'Dehradun', N'248001'),
(37, N'West Bengal', N'19', N'Kolkata', N'700001');

SET IDENTITY_INSERT [dbo].[States] OFF;
GO
