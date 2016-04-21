CREATE TABLE [dbo].[LoggingTable] (
    [Id]        UNIQUEIDENTIFIER NOT NULL,
    [Data]      NVARCHAR (500)   NOT NULL,
    [TimeStamp] DATE             NOT NULL,
    CONSTRAINT [PK_LoggingTable] PRIMARY KEY CLUSTERED ([Id] ASC)
);

