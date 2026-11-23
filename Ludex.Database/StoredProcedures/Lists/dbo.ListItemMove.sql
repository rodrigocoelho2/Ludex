/*
    ListItemMove
    Moves a game of one of the account's lists to @Position (1 = first); the games between shift by one.
*/
CREATE PROCEDURE [dbo].[ListItemMove]
    @UserUniqueID UNIQUEIDENTIFIER,
    @ListID       INT,
    @GameID       INT,
    @Position     INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);

    IF NOT EXISTS (SELECT 1 FROM [dbo].[GameList] WHERE [ListID] = @ListID AND [UserID] = @UserID)
        RETURN;

    BEGIN TRANSACTION;

    DECLARE @From INT = (SELECT [Position] FROM [dbo].[GameListItem] WITH (UPDLOCK, HOLDLOCK) WHERE [ListID] = @ListID AND [GameID] = @GameID);
    DECLARE @Last INT = (SELECT MAX([Position]) FROM [dbo].[GameListItem] WITH (UPDLOCK, HOLDLOCK) WHERE [ListID] = @ListID);
    DECLARE @To INT = CASE WHEN @Position < 1 THEN 1 WHEN @Position > @Last THEN @Last ELSE @Position END;

    IF @From IS NOT NULL AND @To <> @From
    BEGIN
        UPDATE [dbo].[GameListItem]
        SET [Position] = CASE
                WHEN [GameID] = @GameID THEN @To
                WHEN @To < @From AND [Position] >= @To AND [Position] < @From THEN [Position] + 1
                WHEN @To > @From AND [Position] <= @To AND [Position] > @From THEN [Position] - 1
                ELSE [Position] END
        WHERE [ListID] = @ListID;

        UPDATE [dbo].[GameList] SET [UpdatedAt] = SYSUTCDATETIME() WHERE [ListID] = @ListID;
    END

    COMMIT TRANSACTION;
END
