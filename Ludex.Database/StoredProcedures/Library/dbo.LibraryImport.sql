/*
    LibraryImport
    Adds many games to an account's library at once (the Steam, Playnite and PlayStation imports and the daily syncs),
    in one call. The games must already be in dbo.Game. @Games is a JSON array of { GameID, EntryID, Slot, Status,
    Hours, PlayedAt, AppID, RecentMinutes, Platform, Store, Format, AchUnlocked, AchTotal }.
    EntryID is the entry (copy) the row goes into, chosen by the WebApi (the copy on the same platform family); NULL
    (or an entry taken out meanwhile) adds a new entry, so a game on Steam and on PS5 is two entries. Slot tells new
    rows of one game apart (the platform family).
    An entry already there keeps its status, rating and notes; its hours go up (never down: hours played elsewhere
    stay).
    - A row without Platform comes from Steam: the entry remembers its Steam app and its last two weeks, a PC copy with
      no store yet becomes a Steam copy, and a new entry goes in as PC, store Steam (digital).
    - A row with Platform (Playnite, PlayStation) says the copy: an entry with no platform yet takes it, and its store
      and format when it had none (or the same platform); a new entry goes in with that platform, store and format.
    - AchUnlocked/AchTotal (PlayStation trophies) set the achievement counts of entries that aren't Steam copies.
    "Last changed" and (when Finished) "finished" are set to when it was last played rather than now, so the import
    doesn't make everything look recent.
    Every game's hours (its copies together) are kept for today in dbo.PlaytimeDay (the history behind "the last two
    weeks" and the stats). Nothing goes into the friends' feed. Returns how many entries were added and how many got
    more hours.
*/
CREATE PROCEDURE [dbo].[LibraryImport]
    @UserUniqueID UNIQUEIDENTIFIER,
    @Games        NVARCHAR (MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);
    IF @UserID IS NULL
    BEGIN
        SELECT 0 AS [Added], 0 AS [Updated];
        RETURN;
    END

    DECLARE @Now DATETIME2 (3) = SYSUTCDATETIME();
    DECLARE @Changes TABLE ([Action] NVARCHAR (10) NOT NULL, [Before] DECIMAL (7, 1) NULL, [After] DECIMAL (7, 1) NULL);
    DECLARE @Rows TABLE
    (
        [RowID]         INT            IDENTITY (1, 1) PRIMARY KEY,
        [GameID]        INT            NOT NULL,
        [EntryID]       BIGINT         NULL,
        [Status]        TINYINT        NOT NULL,
        [Hours]         DECIMAL (7, 1) NULL,
        [PlayedAt]      DATETIME2 (3)  NOT NULL,
        [AppID]         INT            NULL,
        [RecentMinutes] INT            NULL,
        [Platform]      NVARCHAR (60)  NULL,
        [Store]         NVARCHAR (30)  NULL,
        [Format]        TINYINT        NULL,
        [AchUnlocked]   SMALLINT       NULL,
        [AchTotal]      SMALLINT       NULL
    );

    INSERT INTO @Rows ([GameID], [EntryID], [Status], [Hours], [PlayedAt], [AppID], [RecentMinutes], [Platform], [Store], [Format], [AchUnlocked], [AchTotal])
    SELECT j.[GameID],
           e.[EntryID],
           MAX(j.[Status]),
           MAX(CAST(ROUND(j.[Hours], 1) AS DECIMAL (7, 1))),
           MAX(CASE WHEN j.[PlayedAt] IS NULL OR j.[PlayedAt] > @Now THEN @Now ELSE j.[PlayedAt] END),
           MAX(j.[AppID]),
           MAX(j.[RecentMinutes]),
           MAX(NULLIF(LTRIM(RTRIM(j.[Platform])), N'')),
           MAX(NULLIF(LTRIM(RTRIM(j.[Store])), N'')),
           MAX(CASE WHEN j.[Format] IN (1, 2) THEN j.[Format] END),
           MAX(CASE WHEN j.[AchUnlocked] BETWEEN 0 AND j.[AchTotal] THEN j.[AchUnlocked] END),
           MAX(CASE WHEN j.[AchTotal] BETWEEN 1 AND 32000 THEN j.[AchTotal] END)
    FROM OPENJSON(@Games) WITH
    (
        [GameID]        INT            '$.GameID',
        [EntryID]       BIGINT         '$.EntryID',
        [Slot]          NVARCHAR (40)  '$.Slot',
        [Status]        TINYINT        '$.Status',
        [Hours]         DECIMAL (9, 2) '$.Hours',
        [PlayedAt]      DATETIME2 (3)  '$.PlayedAt',
        [AppID]         INT            '$.AppID',
        [RecentMinutes] INT            '$.RecentMinutes',
        [Platform]      NVARCHAR (60)  '$.Platform',
        [Store]         NVARCHAR (30)  '$.Store',
        [Format]        TINYINT        '$.Format',
        [AchUnlocked]   SMALLINT       '$.AchUnlocked',
        [AchTotal]      SMALLINT       '$.AchTotal'
    ) j
    LEFT JOIN [dbo].[LibraryEntry] e ON e.[EntryID] = j.[EntryID] AND e.[UserID] = @UserID AND e.[GameID] = j.[GameID]
    WHERE j.[Status] BETWEEN 1 AND 6
      AND (j.[Hours] IS NULL OR j.[Hours] BETWEEN 0 AND 99999)
      AND EXISTS (SELECT 1 FROM [dbo].[Game] g WHERE g.[GameID] = j.[GameID])
    GROUP BY j.[GameID], e.[EntryID], CASE WHEN e.[EntryID] IS NULL THEN ISNULL(j.[Slot], N'') END;

    BEGIN TRANSACTION;

    UPDATE t
    SET [Hours] = CASE WHEN s.[Hours] > ISNULL(t.[Hours], 0) THEN s.[Hours] ELSE t.[Hours] END,
        [SteamAppID] = ISNULL(s.[AppID], t.[SteamAppID]),
        [RecentMinutes] = CASE WHEN s.[AppID] IS NULL THEN t.[RecentMinutes] ELSE s.[RecentMinutes] END,
        [Platform] = CASE
                         WHEN s.[Platform] IS NOT NULL THEN ISNULL(t.[Platform], s.[Platform])
                         WHEN t.[Store] IS NULL AND (t.[Platform] IS NULL OR t.[Platform] = N'PC') THEN N'PC'
                         ELSE t.[Platform] END,
        [Store] = CASE
                      WHEN s.[Platform] IS NOT NULL THEN
                          CASE WHEN t.[Store] IS NULL AND t.[Format] IS NULL AND (t.[Platform] IS NULL OR t.[Platform] = s.[Platform]) THEN s.[Store] ELSE t.[Store] END
                      WHEN t.[Store] IS NULL AND (t.[Platform] IS NULL OR t.[Platform] = N'PC') THEN N'Steam'
                      ELSE t.[Store] END,
        [Format] = CASE
                       WHEN s.[Platform] IS NOT NULL THEN
                           CASE WHEN t.[Store] IS NULL AND t.[Format] IS NULL AND (t.[Platform] IS NULL OR t.[Platform] = s.[Platform]) THEN s.[Format] ELSE t.[Format] END
                       WHEN t.[Store] IS NULL AND (t.[Platform] IS NULL OR t.[Platform] = N'PC') THEN 2
                       ELSE t.[Format] END,
        [AchievementsUnlocked] = CASE WHEN s.[AchTotal] IS NOT NULL AND t.[SteamAppID] IS NULL AND s.[AppID] IS NULL THEN s.[AchUnlocked] ELSE t.[AchievementsUnlocked] END,
        [AchievementsTotal] = CASE WHEN s.[AchTotal] IS NOT NULL AND t.[SteamAppID] IS NULL AND s.[AppID] IS NULL THEN s.[AchTotal] ELSE t.[AchievementsTotal] END,
        [AchievementsAt] = CASE WHEN s.[AchTotal] IS NOT NULL AND t.[SteamAppID] IS NULL AND s.[AppID] IS NULL THEN @Now ELSE t.[AchievementsAt] END,
        [UpdatedAt] = CASE WHEN s.[Hours] > ISNULL(t.[Hours], 0) AND s.[PlayedAt] > t.[UpdatedAt] THEN s.[PlayedAt] ELSE t.[UpdatedAt] END
    OUTPUT N'UPDATE', deleted.[Hours], inserted.[Hours] INTO @Changes ([Action], [Before], [After])
    FROM [dbo].[LibraryEntry] t
    INNER JOIN @Rows s ON s.[EntryID] = t.[EntryID]
    WHERE t.[UserID] = @UserID;

    INSERT INTO [dbo].[LibraryEntry] ([UserID], [GameID], [Status], [Hours], [Platform], [Store], [Format], [SteamAppID], [RecentMinutes],
                                      [AchievementsUnlocked], [AchievementsTotal], [AchievementsAt], [UpdatedAt], [FinishedAt])
    OUTPUT N'INSERT', NULL, inserted.[Hours] INTO @Changes ([Action], [Before], [After])
    SELECT @UserID, s.[GameID], s.[Status], NULLIF(s.[Hours], 0),
           ISNULL(s.[Platform], N'PC'),
           CASE WHEN s.[Platform] IS NULL THEN N'Steam' ELSE s.[Store] END,
           CASE WHEN s.[Platform] IS NULL THEN 2 ELSE s.[Format] END,
           s.[AppID], s.[RecentMinutes],
           CASE WHEN s.[AchTotal] IS NOT NULL THEN s.[AchUnlocked] END, s.[AchTotal], CASE WHEN s.[AchTotal] IS NOT NULL THEN @Now END,
           s.[PlayedAt], CASE WHEN s.[Status] = 2 THEN s.[PlayedAt] END
    FROM @Rows s
    WHERE s.[EntryID] IS NULL;

    -- Today's hours of every game (its copies together), for the history.
    MERGE [dbo].[PlaytimeDay] WITH (HOLDLOCK) AS t
    USING (SELECT e.[GameID], SUM(e.[Hours]) AS [Hours]
           FROM [dbo].[LibraryEntry] e
           WHERE e.[UserID] = @UserID AND e.[Hours] IS NOT NULL
             AND e.[GameID] IN (SELECT r.[GameID] FROM @Rows r)
           GROUP BY e.[GameID]) AS s
    ON t.[UserID] = @UserID AND t.[GameID] = s.[GameID] AND t.[Day] = CAST(@Now AS DATE)
    WHEN MATCHED THEN UPDATE SET [Hours] = s.[Hours]
    WHEN NOT MATCHED THEN INSERT ([UserID], [GameID], [Day], [Hours]) VALUES (@UserID, s.[GameID], CAST(@Now AS DATE), s.[Hours]);

    COMMIT TRANSACTION;

    SELECT ISNULL(SUM(CASE WHEN [Action] = N'INSERT' THEN 1 ELSE 0 END), 0) AS [Added],
           ISNULL(SUM(CASE WHEN [Action] = N'UPDATE' AND ISNULL([After], 0) > ISNULL([Before], 0) THEN 1 ELSE 0 END), 0) AS [Updated]
    FROM @Changes;
END
