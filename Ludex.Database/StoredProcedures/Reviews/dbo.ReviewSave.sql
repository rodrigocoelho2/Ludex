/*
    ReviewSave
    Writes an account's review of a game, or changes it (one review per account and game), and returns its id. The
    rating, status, ending and platform are copied from the log at the time. The first time it is written, the friends'
    feed gets a "reviewed" line.
*/
CREATE PROCEDURE [dbo].[ReviewSave]
    @UserUniqueID UNIQUEIDENTIFIER,
    @GameID       INT,
    @Text         NVARCHAR (4000),
    @Spoilers     BIT,
    @Rating       TINYINT,
    @Status       TINYINT,
    @Ending       TINYINT,
    @Platform     NVARCHAR (60),
    @At           DATETIME2 (3) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);
    IF @UserID IS NULL
        RETURN;

    DECLARE @When DATETIME2 (3) = COALESCE(@At, SYSUTCDATETIME());
    DECLARE @ReviewID BIGINT;

    BEGIN TRANSACTION;

    SELECT @ReviewID = [ReviewID]
    FROM [dbo].[Review] WITH (UPDLOCK, HOLDLOCK)
    WHERE [UserID] = @UserID AND [GameID] = @GameID;

    IF @ReviewID IS NULL
    BEGIN
        INSERT INTO [dbo].[Review] ([UserID], [GameID], [Rating], [Status], [Ending], [Platform], [Text], [Spoilers], [CreatedAt], [UpdatedAt])
        VALUES (@UserID, @GameID, @Rating, @Status, @Ending, @Platform, @Text, @Spoilers, @When, @When);
        SET @ReviewID = CAST(SCOPE_IDENTITY() AS BIGINT);

        INSERT INTO [dbo].[Activity] ([UserID], [GameID], [Kind], [Rating], [At])
        VALUES (@UserID, @GameID, 8, @Rating, @When);
    END
    ELSE
        UPDATE [dbo].[Review]
        SET [Rating] = @Rating,
            [Status] = @Status,
            [Ending] = @Ending,
            [Platform] = @Platform,
            [Text] = @Text,
            [Spoilers] = @Spoilers,
            [UpdatedAt] = @When
        WHERE [ReviewID] = @ReviewID;

    COMMIT TRANSACTION;

    SELECT @ReviewID AS [ReviewID];
END
