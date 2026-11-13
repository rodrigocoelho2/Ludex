/*
    PsnTitleRemember
    Remembers PlayStation titles as seen (@Titles: a JSON array of { TitleKey, Name, GameID }). A title already there
    keeps its game unless a game is given (a match found later), and the account counts as synced.
*/
CREATE PROCEDURE [dbo].[PsnTitleRemember]
    @UserUniqueID UNIQUEIDENTIFIER,
    @Titles       NVARCHAR (MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);
    IF @UserID IS NULL
        RETURN;

    MERGE [dbo].[PsnTitle] WITH (HOLDLOCK) AS t
    USING (
        SELECT j.[TitleKey], MAX(LEFT(ISNULL(j.[Name], N''), 200)) AS [Name], MAX(g.[GameID]) AS [GameID]
        FROM OPENJSON(@Titles) WITH
        (
            [TitleKey] NVARCHAR (100) '$.TitleKey',
            [Name]     NVARCHAR (400) '$.Name',
            [GameID]   INT            '$.GameID'
        ) j
        LEFT JOIN [dbo].[Game] g ON g.[GameID] = j.[GameID]
        WHERE j.[TitleKey] IS NOT NULL AND LEN(j.[TitleKey]) > 0
        GROUP BY j.[TitleKey]
    ) AS s
    ON t.[UserID] = @UserID AND t.[TitleKey] = s.[TitleKey]
    WHEN MATCHED AND s.[GameID] IS NOT NULL AND ISNULL(t.[GameID], 0) <> s.[GameID] THEN
        UPDATE SET [GameID] = s.[GameID]
    WHEN NOT MATCHED THEN
        INSERT ([UserID], [TitleKey], [Name], [GameID]) VALUES (@UserID, s.[TitleKey], s.[Name], s.[GameID]);

    UPDATE [dbo].[PsnAccount] SET [SyncedAt] = SYSUTCDATETIME() WHERE [UserID] = @UserID;
END
