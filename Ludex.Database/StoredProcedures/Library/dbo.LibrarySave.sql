/*
    LibrarySave
    Adds a game to an account's library or changes one of its entries, and returns the entry's id. The game must
    already be in dbo.Game (GameUpsert).
    - @EntryID given: that entry (added again when it was taken out meanwhile).
    - @EntryID NULL: the game's first entry, or a new one when the game isn't there.
    - @NewCopy = 1: always a new entry (the same game on another platform: its own status, hours and achievements).
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
    @Format       TINYINT = NULL,
    @EntryID      BIGINT = NULL,
    @NewCopy      BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);
    IF @UserID IS NULL
        RETURN;

    BEGIN TRANSACTION;

    DECLARE @Target BIGINT = NULL;
    IF @NewCopy = 0
        SELECT TOP (1) @Target = e.[EntryID]
        FROM [dbo].[LibraryEntry] e WITH (UPDLOCK, HOLDLOCK)
        WHERE e.[UserID] = @UserID AND e.[GameID] = @GameID AND (@EntryID IS NULL OR e.[EntryID] = @EntryID)
        ORDER BY e.[EntryID];

    IF @Target IS NULL
    BEGIN
        INSERT INTO [dbo].[LibraryEntry] ([UserID], [GameID], [Status], [Rating], [Hours], [Platform], [Store], [Format], [Notes], [FinishedAt])
        VALUES (@UserID, @GameID, @Status, @Rating, @Hours, @Platform, @Store, @Format, @Notes, CASE WHEN @Status = 2 THEN SYSUTCDATETIME() END);
        SET @Target = CAST(SCOPE_IDENTITY() AS BIGINT);
    END
    ELSE
        UPDATE [dbo].[LibraryEntry]
        SET [Status] = @Status,
            [Rating] = @Rating,
            [Hours] = @Hours,
            [Platform] = @Platform,
            [Store] = @Store,
            [Format] = @Format,
            [Notes] = @Notes,
            [UpdatedAt] = SYSUTCDATETIME(),
            [FinishedAt] = CASE WHEN [FinishedAt] IS NULL AND @Status = 2 THEN SYSUTCDATETIME() ELSE [FinishedAt] END
        WHERE [EntryID] = @Target;

    COMMIT TRANSACTION;

    SELECT @Target AS [EntryID];
END
