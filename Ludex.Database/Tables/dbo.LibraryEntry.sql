CREATE TABLE [dbo].[LibraryEntry]
(
    [UserID]     BIGINT          NOT NULL,
    [GameID]     INT             NOT NULL,
    [Status]     TINYINT         NOT NULL,
    [Rating]     TINYINT         NULL,
    [Hours]      DECIMAL (7, 1)  NULL,
    [Platform]   NVARCHAR (60)   NULL,
    [Store]      NVARCHAR (30)   NULL,
    [Format]     TINYINT         NULL,
    [SteamAppID]           INT            NULL,
    [RecentMinutes]        INT            NULL,
    [AchievementsUnlocked] SMALLINT       NULL,
    [AchievementsTotal]    SMALLINT       NULL,
    [AchievementsAt]       DATETIME2 (3)  NULL,
    [PriceTarget]          DECIMAL (9, 2) NULL,
    [PriceAlerted]         DECIMAL (9, 2) NULL,
    [Notes]      NVARCHAR (2000) NULL,
    [AddedAt]    DATETIME2 (3)   NOT NULL CONSTRAINT [DF_LibraryEntry_AddedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]  DATETIME2 (3)   NOT NULL CONSTRAINT [DF_LibraryEntry_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [FinishedAt] DATETIME2 (3)   NULL,
    CONSTRAINT [PK_LibraryEntry] PRIMARY KEY CLUSTERED ([UserID] ASC, [GameID] ASC),
    CONSTRAINT [FK_LibraryEntry_User] FOREIGN KEY ([UserID]) REFERENCES [dbo].[User] ([UserID]) ON DELETE CASCADE,
    CONSTRAINT [FK_LibraryEntry_Game] FOREIGN KEY ([GameID]) REFERENCES [dbo].[Game] ([GameID]),
    CONSTRAINT [CK_LibraryEntry_Status] CHECK ([Status] BETWEEN 1 AND 6),
    CONSTRAINT [CK_LibraryEntry_Rating] CHECK ([Rating] IS NULL OR [Rating] BETWEEN 1 AND 10),
    CONSTRAINT [CK_LibraryEntry_Hours] CHECK ([Hours] IS NULL OR [Hours] >= 0),
    CONSTRAINT [CK_LibraryEntry_Format] CHECK ([Format] IS NULL OR [Format] IN (1, 2))
);
GO

CREATE NONCLUSTERED INDEX [IX_LibraryEntry_GameID]
    ON [dbo].[LibraryEntry] ([GameID] ASC)
    INCLUDE ([Status], [Rating]);
GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'One game in one account''s library. Status: 1 Playing, 2 Finished, 3 Wishlist, 4 Dropped, 5 Backlog, 6 Played (owned, not started). Rating 1–10. FinishedAt is set the first time the status becomes Finished. Store: where a PC copy was bought (Steam, Epic Games, GOG…). Format: 1 Physical, 2 Digital (the WebApi checks it against the platform). SteamAppID, RecentMinutes (Steam''s last two weeks) and the achievement counts come from the Steam sync. PriceTarget: alert when the best price is at or under it; PriceAlerted: the price last alerted.',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'LibraryEntry';
