-- =============================================
-- Author: Antigravity (AI)
-- Create date: 2026-04-11
-- Description: Database set up for Centralized logging 
-- =============================================

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'ErrorLogsDb')
BEGIN
    CREATE DATABASE ErrorLogsDb;
END
GO

USE ErrorLogsDb;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AppLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AppLogs](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Message] [nvarchar](max) NULL,
        [MessageTemplate] [nvarchar](max) NULL,
        [Level] [nvarchar](128) NULL,
        [TimeStamp] [datetimeoffset](7) NOT NULL,
        [Exception] [nvarchar](max) NULL,
        [Properties] [xml] NULL,
        [LogEvent] [nvarchar](max) NULL,
        [ServiceName] [nvarchar](100) NULL, 
        [CorrelationId] [nvarchar](100) NULL 

        CONSTRAINT [PK_AppLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO
