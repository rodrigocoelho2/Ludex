CREATE TABLE [dbo].[Collection]
(
    [CollectionID] INT            IDENTITY (1, 1) NOT NULL,
    [UserID]       BIGINT         NOT NULL,
    [Name]         NVARCHAR (60)  NOT NULL,
    [CreatedAt]    DATETIME2 (3)  NOT NULL CONSTRAINT [DF_Collection_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Collection] PRIMARY KEY CLUSTERED ([CollectionID] ASC),
    CONSTRAINT [FK_Collection_User] FOREIGN KEY ([UserID]) REFERENCES [dbo].[User] ([UserID]) ON DELETE CASCADE,
    CONSTRAINT [UQ_Collection_Name] UNIQUE ([UserID], [Name])
);
GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'An account''s own groups of games (like Steam''s collections: "Favourites", "Souls-like").',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'Collection';
