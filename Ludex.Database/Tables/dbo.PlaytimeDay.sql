CREATE TABLE [dbo].[PlaytimeDay]
(
    [UserID] BIGINT         NOT NULL,
    [GameID] INT            NOT NULL,
    [Day]    DATE           NOT NULL,
    [Hours]  DECIMAL (7, 1) NOT NULL,
    CONSTRAINT [PK_PlaytimeDay] PRIMARY KEY CLUSTERED ([UserID] ASC, [GameID] ASC, [Day] ASC),
    CONSTRAINT [FK_PlaytimeDay_User] FOREIGN KEY ([UserID]) REFERENCES [dbo].[User] ([UserID]) ON DELETE CASCADE,
    CONSTRAINT [FK_PlaytimeDay_Game] FOREIGN KEY ([GameID]) REFERENCES [dbo].[Game] ([GameID])
);
GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'The hours of each game on each day the Steam sync ran: the history behind playtime over time.',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'PlaytimeDay';
