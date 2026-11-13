CREATE TABLE [dbo].[PsnTitle]
(
    [UserID]      BIGINT         NOT NULL,
    [TitleKey]    NVARCHAR (100) NOT NULL,
    [Name]        NVARCHAR (200) NOT NULL,
    [GameID]      INT            NULL,
    [FirstSeenAt] DATETIME2 (3)  NOT NULL CONSTRAINT [DF_PsnTitle_FirstSeenAt] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_PsnTitle] PRIMARY KEY CLUSTERED ([UserID] ASC, [TitleKey] ASC),
    CONSTRAINT [FK_PsnTitle_User] FOREIGN KEY ([UserID]) REFERENCES [dbo].[User] ([UserID]) ON DELETE CASCADE,
    CONSTRAINT [FK_PsnTitle_Game] FOREIGN KEY ([GameID]) REFERENCES [dbo].[Game] ([GameID])
);
GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'The PlayStation titles an account had when it last imported or synced, and the game each one is (NULL: IGDB has no match). TitleKey is the PSN concept (one per game across PS4 and PS5), or the title id when there is none. The daily sync only adds titles played after this, so titles left out on purpose stay out.',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'PsnTitle';
