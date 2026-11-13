/*
    ProfilePlayDays
    How long an account played each day since @From, for "My year in games": the hours a game's daily snapshot in
    dbo.PlaytimeDay (written by the imports and the daily syncs) went up by since its snapshot before. A game's first
    snapshot says nothing about that day (its hours could be years old), so it counts as nothing.
*/
CREATE PROCEDURE [dbo].[ProfilePlayDays]
    @UserUniqueID UNIQUEIDENTIFIER,
    @From         DATE
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);

    WITH Snapshots AS
    (
        SELECT d.[Day], d.[Hours], LAG(d.[Hours]) OVER (PARTITION BY d.[GameID] ORDER BY d.[Day]) AS [Before]
        FROM [dbo].[PlaytimeDay] d
        WHERE d.[UserID] = @UserID
    )
    SELECT s.[Day], CAST(SUM(s.[Hours] - s.[Before]) AS DECIMAL (7, 1)) AS [Hours]
    FROM Snapshots s
    WHERE s.[Day] >= @From AND s.[Before] IS NOT NULL AND s.[Hours] > s.[Before]
    GROUP BY s.[Day]
    ORDER BY s.[Day];
END
