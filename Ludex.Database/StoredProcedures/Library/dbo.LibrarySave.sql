/*
    LibrarySave
    Adds a game to an account's library or changes it. The game must already be in dbo.Game (GameUpsert).
    FinishedAt is set the first time the status becomes Finished (2) and kept afterwards.
*/
CREATE PROCEDURE [dbo].[LibrarySave]
    @UserUniqueID UNIQUEIDENTIFIER,
    @GameID       INT,
    @Status       TINYINT,
    @Rating       TINYINT,
    @Hours        DECIMAL (7, 1),
    @Platform     NVARCHAR (60),
    @Notes        NVARCHAR (2000)
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
                   [Notes] = @Notes,
                   [UpdatedAt] = SYSUTCDATETIME(),
                   [FinishedAt] = CASE WHEN t.[FinishedAt] IS NULL AND @Status = 2 THEN SYSUTCDATETIME() ELSE t.[FinishedAt] END
    WHEN NOT MATCHED THEN
        INSERT ([UserID], [GameID], [Status], [Rating], [Hours], [Platform], [Notes], [FinishedAt])
        VALUES (@UserID, @GameID, @Status, @Rating, @Hours, @Platform, @Notes, CASE WHEN @Status = 2 THEN SYSUTCDATETIME() END);
END
