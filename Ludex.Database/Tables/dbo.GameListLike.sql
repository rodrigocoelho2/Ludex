CREATE TABLE [dbo].[GameListLike]
(
    [ListID] INT           NOT NULL,
    [UserID] BIGINT        NOT NULL,
    [At]     DATETIME2 (3) NOT NULL CONSTRAINT [DF_GameListLike_At] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_GameListLike] PRIMARY KEY CLUSTERED ([ListID] ASC, [UserID] ASC),
    CONSTRAINT [FK_GameListLike_List] FOREIGN KEY ([ListID]) REFERENCES [dbo].[GameList] ([ListID]) ON DELETE CASCADE,
    CONSTRAINT [FK_GameListLike_User] FOREIGN KEY ([UserID]) REFERENCES [dbo].[User] ([UserID])
);
GO

CREATE NONCLUSTERED INDEX [IX_GameListLike_UserID]
    ON [dbo].[GameListLike] ([UserID] ASC);
GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Who liked which list. The likes go with the list; an account''s own likes are removed by UserDelete.',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'GameListLike';
