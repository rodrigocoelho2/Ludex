CREATE TABLE [dbo].[CollectionGame]
(
    [CollectionID] INT           NOT NULL,
    [GameID]       INT           NOT NULL,
    [AddedAt]      DATETIME2 (3) NOT NULL CONSTRAINT [DF_CollectionGame_AddedAt] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_CollectionGame] PRIMARY KEY CLUSTERED ([CollectionID] ASC, [GameID] ASC),
    CONSTRAINT [FK_CollectionGame_Collection] FOREIGN KEY ([CollectionID]) REFERENCES [dbo].[Collection] ([CollectionID]) ON DELETE CASCADE,
    CONSTRAINT [FK_CollectionGame_Game] FOREIGN KEY ([GameID]) REFERENCES [dbo].[Game] ([GameID])
);
GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Which games are in which collection. A game taken out of the library stays in its collections (it shows again if added back).',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'CollectionGame';
