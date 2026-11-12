CREATE TABLE [dbo].[Activity]
(
    [ActivityID] BIGINT        IDENTITY (1, 1) NOT NULL,
    [UserID]     BIGINT        NOT NULL,
    [GameID]     INT           NOT NULL,
    [Kind]       TINYINT       NOT NULL,
    [Rating]     TINYINT       NULL,
    [At]         DATETIME2 (3) NOT NULL CONSTRAINT [DF_Activity_At] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Activity] PRIMARY KEY CLUSTERED ([ActivityID] ASC),
    CONSTRAINT [FK_Activity_User] FOREIGN KEY ([UserID]) REFERENCES [dbo].[User] ([UserID]) ON DELETE CASCADE,
    CONSTRAINT [FK_Activity_Game] FOREIGN KEY ([GameID]) REFERENCES [dbo].[Game] ([GameID])
);
GO

CREATE NONCLUSTERED INDEX [IX_Activity_UserID_At]
    ON [dbo].[Activity] ([UserID] ASC, [At] DESC)
    INCLUDE ([GameID], [Kind], [Rating]);
GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'What each account did with its games, for the friends'' feed. Kind: 1 Added, 2 Started, 3 Finished, 4 Rated, 5 Wishlisted, 6 Dropped, 7 Played.',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'Activity';
