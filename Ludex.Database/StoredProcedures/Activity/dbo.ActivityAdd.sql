/*
    ActivityAdd
    Records something an account did with a game. The same kind for the same game within ten minutes replaces the
    earlier line (rating 7, then 8, then 9 in a row shows once, as 9).
*/
CREATE PROCEDURE [dbo].[ActivityAdd]
    @UserUniqueID UNIQUEIDENTIFIER,
    @GameID       INT,
    @Kind         TINYINT,
    @Rating       TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);
    IF @UserID IS NULL
        RETURN;

    BEGIN TRANSACTION;

    DELETE FROM [dbo].[Activity]
    WHERE [UserID] = @UserID AND [GameID] = @GameID AND [Kind] = @Kind
      AND [At] > DATEADD(MINUTE, -10, SYSUTCDATETIME());

    INSERT INTO [dbo].[Activity] ([UserID], [GameID], [Kind], [Rating])
    VALUES (@UserID, @GameID, @Kind, @Rating);

    COMMIT TRANSACTION;
END
