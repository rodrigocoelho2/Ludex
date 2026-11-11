/*
    LibraryImport
    Adds many games to an account's library at once (the Steam import), in one call. The games must already be in
    dbo.Game. @Games is a JSON array of { GameID, Status, Hours, PlayedAt }.
    A game already there keeps its status, rating and notes; its hours go up (never down: hours played elsewhere stay),
    and a PC copy with no store yet becomes a Steam copy. A new game goes in with the status chosen, platform PC, store
    Steam (digital), "last changed" and (when Finished)
    "finished" set to when it was last played rather than now, so the import doesn't make everything look recent.
    Nothing goes into the friends' feed. Returns how many games were added and how many were updated.
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
    DECLARE @Changes TABLE ([Action] NVARCHAR (10) NOT NULL);

    BEGIN TRANSACTION;

    MERGE [dbo].[LibraryEntry] WITH (HOLDLOCK) AS t
    USING
    (
        SELECT j.[GameID],
               j.[Status],
               CAST(ROUND(j.[Hours], 1) AS DECIMAL (7, 1)) AS [Hours],
               CASE WHEN j.[PlayedAt] IS NULL OR j.[PlayedAt] > @Now THEN @Now ELSE j.[PlayedAt] END AS [PlayedAt]
        FROM OPENJSON(@Games) WITH
        (
            [GameID]   INT            '$.GameID',
            [Status]   TINYINT        '$.Status',
            [Hours]    DECIMAL (9, 2) '$.Hours',
            [PlayedAt] DATETIME2 (3)  '$.PlayedAt'
        ) j
        WHERE j.[Status] BETWEEN 1 AND 4
          AND (j.[Hours] IS NULL OR j.[Hours] BETWEEN 0 AND 99999)
          AND EXISTS (SELECT 1 FROM [dbo].[Game] g WHERE g.[GameID] = j.[GameID])
    ) AS s
    ON t.[UserID] = @UserID AND t.[GameID] = s.[GameID]
    WHEN MATCHED AND (s.[Hours] > ISNULL(t.[Hours], 0) OR (t.[Store] IS NULL AND (t.[Platform] IS NULL OR t.[Platform] = N'PC'))) THEN
        UPDATE SET [Hours] = CASE WHEN s.[Hours] > ISNULL(t.[Hours], 0) THEN s.[Hours] ELSE t.[Hours] END,
                   [Platform] = ISNULL(t.[Platform], N'PC'),
                   [Store] = CASE WHEN t.[Store] IS NULL AND (t.[Platform] IS NULL OR t.[Platform] = N'PC') THEN N'Steam' ELSE t.[Store] END,
                   [Format] = CASE WHEN t.[Store] IS NULL AND (t.[Platform] IS NULL OR t.[Platform] = N'PC') THEN 2 ELSE t.[Format] END,
                   [UpdatedAt] = CASE WHEN s.[Hours] > ISNULL(t.[Hours], 0) AND s.[PlayedAt] > t.[UpdatedAt] THEN s.[PlayedAt] ELSE t.[UpdatedAt] END
    WHEN NOT MATCHED THEN
        INSERT ([UserID], [GameID], [Status], [Hours], [Platform], [Store], [Format], [UpdatedAt], [FinishedAt])
        VALUES (@UserID, s.[GameID], s.[Status], NULLIF(s.[Hours], 0), N'PC', N'Steam', 2, s.[PlayedAt], CASE WHEN s.[Status] = 2 THEN s.[PlayedAt] END)
    OUTPUT $action INTO @Changes;

    COMMIT TRANSACTION;

    SELECT ISNULL(SUM(CASE WHEN [Action] = N'INSERT' THEN 1 ELSE 0 END), 0) AS [Added],
           ISNULL(SUM(CASE WHEN [Action] = N'UPDATE' THEN 1 ELSE 0 END), 0) AS [Updated]
    FROM @Changes;
END
