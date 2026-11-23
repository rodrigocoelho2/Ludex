CREATE TABLE [dbo].[PlaySession]
(
    [SessionID] BIGINT         IDENTITY (1, 1) NOT NULL,
    [UserID]    BIGINT         NOT NULL,
    [GameID]    INT            NOT NULL,
    [Day]       DATE           NOT NULL,
    [Minutes]   INT            NOT NULL,
    [Platform]  NVARCHAR (60)  NULL,
    [Note]      NVARCHAR (500) NULL,
    [CreatedAt] DATETIME2 (3)  NOT NULL CONSTRAINT [DF_PlaySession_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_PlaySession] PRIMARY KEY CLUSTERED ([SessionID] ASC),
    CONSTRAINT [FK_PlaySession_User] FOREIGN KEY ([UserID]) REFERENCES [dbo].[User] ([UserID]) ON DELETE CASCADE,
    CONSTRAINT [FK_PlaySession_Game] FOREIGN KEY ([GameID]) REFERENCES [dbo].[Game] ([GameID]),
    CONSTRAINT [CK_PlaySession_Minutes] CHECK ([Minutes] BETWEEN 0 AND 1440)
);
GO

CREATE NONCLUSTERED INDEX [IX_PlaySession_UserID_Day]
    ON [dbo].[PlaySession] ([UserID] ASC, [Day] DESC)
    INCLUDE ([GameID], [Minutes]);
GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'The journal: play sessions an account wrote down (a day, how long, a note). The journal also shows the time the Steam and PlayStation syncs saw (dbo.PlaytimeDay), which isn''t copied here.',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'PlaySession';
