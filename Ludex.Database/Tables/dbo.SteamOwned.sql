CREATE TABLE [dbo].[SteamOwned]
(
    [UserID]      BIGINT        NOT NULL,
    [AppID]       INT           NOT NULL,
    [FirstSeenAt] DATETIME2 (3) NOT NULL CONSTRAINT [DF_SteamOwned_FirstSeenAt] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_SteamOwned] PRIMARY KEY CLUSTERED ([UserID] ASC, [AppID] ASC),
    CONSTRAINT [FK_SteamOwned_User] FOREIGN KEY ([UserID]) REFERENCES [dbo].[User] ([UserID]) ON DELETE CASCADE
);
GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'The Steam apps an account already had when it last imported or synced. The daily sync only adds games bought since (apps not here), so games left out on purpose stay out.',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'SteamOwned';
