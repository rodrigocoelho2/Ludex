/*
    LibrarySave
    Adds a game to an account's library or changes it. The game must already be in dbo.Game (GameUpsert).
    FinishedAt is set the first time the status becomes Finished (2) and kept afterwards. Store and Format (1 Physical,
    2 Digital) say which copy is owned; the WebApi checks them against the platform.
*/
CREATE PROCEDURE [dbo].[LibrarySave]
    @UserUniqueID UNIQUEIDENTIFIER,
    @GameID       INT,
    @Status       TINYINT,
    @Rating       TINYINT,
    @Hours        DECIMAL (7, 1),
    @Platform     NVARCHAR (60),
    @Notes        NVARCHAR (2000),
    @Store        NVARCHAR (30) = NULL,
    @Format       TINYINT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);
    IF @UserID IS NULL
        RETURN;

    MERGE [dbo].[LibraryEntry] WITH (HOLDLOCK) AS t
    USING (SELECT @UserID AS [UserID], @GameID AS [GameID]) AS s
    ON t.[UserID] = s.[UserID] AND t.[GameID] = s.[GameID]
    WHEN MATCHED THEN
        UPDATE SET [Status] = @Status,
                   [Rating] = @Rating,
                   [Hours] = @Hours,
                   [Platform] = @Platform,
                   [Store] = @Store,
                   [Format] = @Format,
                   [Notes] = @Notes,
                   [UpdatedAt] = SYSUTCDATETIME(),
                   [FinishedAt] = CASE WHEN t.[FinishedAt] IS NULL AND @Status = 2 THEN SYSUTCDATETIME() ELSE t.[FinishedAt] END
    WHEN NOT MATCHED THEN
        INSERT ([UserID], [GameID], [Status], [Rating], [Hours], [Platform], [Store], [Format], [Notes], [FinishedAt])
        VALUES (@UserID, @GameID, @Status, @Rating, @Hours, @Platform, @Store, @Format, @Notes, CASE WHEN @Status = 2 THEN SYSUTCDATETIME() END);
END
