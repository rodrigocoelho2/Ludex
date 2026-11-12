/*
    LibraryImport
    Adds many games to an account's library at once (the Steam import and the daily Steam sync), in one call. The
    games must already be in dbo.Game. @Games is a JSON array of { GameID, Status, Hours, PlayedAt, AppID, RecentMinutes }.
    A game already there keeps its status, rating and notes; its hours go up (never down: hours played elsewhere
    stay), it remembers its Steam app and its last two weeks, and a PC copy with no store yet becomes a Steam copy.
    A new game goes in with the status chosen, platform PC, store Steam (digital), "last changed" and (when Finished)
    "finished" set to when it was last played rather than now, so the import doesn't make everything look recent.
    Every game's hours are kept for today in dbo.PlaytimeDay (the history behind "the last two weeks" and the stats).
    Nothing goes into the friends' feed. Returns how many games were added and how many got more hours.
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
        [GameID]        INT            NOT NULL PRIMARY KEY,
        [Status]        TINYINT        NOT NULL,
        [Hours]         DECIMAL (7, 1) NULL,
        [PlayedAt]      DATETIME2 (3)  NOT NULL,
        [AppID]         INT            NULL,
        [RecentMinutes] INT            NULL
    );

    INSERT INTO @Rows ([GameID], [Status], [Hours], [PlayedAt], [AppID], [RecentMinutes])
    SELECT j.[GameID],
           MAX(j.[Status]),
           MAX(CAST(ROUND(j.[Hours], 1) AS DECIMAL (7, 1))),
           MAX(CASE WHEN j.[PlayedAt] IS NULL OR j.[PlayedAt] > @Now THEN @Now ELSE j.[PlayedAt] END),
           MAX(j.[AppID]),
           MAX(j.[RecentMinutes])
    FROM OPENJSON(@Games) WITH
    (
        [GameID]        INT            '$.GameID',
        [Status]        TINYINT        '$.Status',
        [Hours]         DECIMAL (9, 2) '$.Hours',
        [PlayedAt]      DATETIME2 (3)  '$.PlayedAt',
        [AppID]         INT            '$.AppID',
        [RecentMinutes] INT            '$.RecentMinutes'
    ) j
    WHERE j.[Status] BETWEEN 1 AND 6
      AND (j.[Hours] IS NULL OR j.[Hours] BETWEEN 0 AND 99999)
      AND EXISTS (SELECT 1 FROM [dbo].[Game] g WHERE g.[GameID] = j.[GameID])
    GROUP BY j.[GameID];

    BEGIN TRANSACTION;

    MERGE [dbo].[LibraryEntry] WITH (HOLDLOCK) AS t
    USING @Rows AS s
    ON t.[UserID] = @UserID AND t.[GameID] = s.[GameID]
    WHEN MATCHED THEN
        UPDATE SET [Hours] = CASE WHEN s.[Hours] > ISNULL(t.[Hours], 0) THEN s.[Hours] ELSE t.[Hours] END,
                   [SteamAppID] = ISNULL(s.[AppID], t.[SteamAppID]),
                   [RecentMinutes] = CASE WHEN s.[AppID] IS NULL THEN t.[RecentMinutes] ELSE s.[RecentMinutes] END,
                   [Platform] = CASE WHEN t.[Store] IS NULL AND (t.[Platform] IS NULL OR t.[Platform] = N'PC') THEN N'PC' ELSE t.[Platform] END,
                   [Store] = CASE WHEN t.[Store] IS NULL AND (t.[Platform] IS NULL OR t.[Platform] = N'PC') THEN N'Steam' ELSE t.[Store] END,
                   [Format] = CASE WHEN t.[Store] IS NULL AND (t.[Platform] IS NULL OR t.[Platform] = N'PC') THEN 2 ELSE t.[Format] END,
                   [UpdatedAt] = CASE WHEN s.[Hours] > ISNULL(t.[Hours], 0) AND s.[PlayedAt] > t.[UpdatedAt] THEN s.[PlayedAt] ELSE t.[UpdatedAt] END
    WHEN NOT MATCHED THEN
        INSERT ([UserID], [GameID], [Status], [Hours], [Platform], [Store], [Format], [SteamAppID], [RecentMinutes], [UpdatedAt], [FinishedAt])
        VALUES (@UserID, s.[GameID], s.[Status], NULLIF(s.[Hours], 0), N'PC', N'Steam', 2, s.[AppID], s.[RecentMinutes], s.[PlayedAt],
                CASE WHEN s.[Status] = 2 THEN s.[PlayedAt] END)
    OUTPUT $action, deleted.[Hours], inserted.[Hours] INTO @Changes ([Action], [Before], [After]);

    -- Today's hours of every game, for the history.
    MERGE [dbo].[PlaytimeDay] WITH (HOLDLOCK) AS t
    USING (SELECT r.[GameID], e.[Hours]
           FROM @Rows r
           INNER JOIN [dbo].[LibraryEntry] e ON e.[UserID] = @UserID AND e.[GameID] = r.[GameID]
           WHERE e.[Hours] IS NOT NULL) AS s
    ON t.[UserID] = @UserID AND t.[GameID] = s.[GameID] AND t.[Day] = CAST(@Now AS DATE)
    WHEN MATCHED THEN UPDATE SET [Hours] = s.[Hours]
    WHEN NOT MATCHED THEN INSERT ([UserID], [GameID], [Day], [Hours]) VALUES (@UserID, s.[GameID], CAST(@Now AS DATE), s.[Hours]);

    COMMIT TRANSACTION;

    SELECT ISNULL(SUM(CASE WHEN [Action] = N'INSERT' THEN 1 ELSE 0 END), 0) AS [Added],
           ISNULL(SUM(CASE WHEN [Action] = N'UPDATE' AND ISNULL([After], 0) > ISNULL([Before], 0) THEN 1 ELSE 0 END), 0) AS [Updated]
    FROM @Changes;
END
