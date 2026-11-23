CREATE TABLE [dbo].[GameListItem]
(
    [ListID]   INT            NOT NULL,
    [GameID]   INT            NOT NULL,
    [Position] INT            NOT NULL,
    [Note]     NVARCHAR (300) NULL,
    [AddedAt]  DATETIME2 (3)  NOT NULL CONSTRAINT [DF_GameListItem_AddedAt] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_GameListItem] PRIMARY KEY CLUSTERED ([ListID] ASC, [GameID] ASC),
    CONSTRAINT [FK_GameListItem_List] FOREIGN KEY ([ListID]) REFERENCES [dbo].[GameList] ([ListID]) ON DELETE CASCADE,
    CONSTRAINT [FK_GameListItem_Game] FOREIGN KEY ([GameID]) REFERENCES [dbo].[Game] ([GameID])
);
GO

CREATE NONCLUSTERED INDEX [IX_GameListItem_GameID]
    ON [dbo].[GameListItem] ([GameID] ASC);
GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'The games of a list, in order (Position 1, 2, 3…), each with an optional note.',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'GameListItem';
