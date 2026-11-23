/*
    SessionAdd
    Writes down a play session in the journal and returns its id.
*/
CREATE PROCEDURE [dbo].[SessionAdd]
    @UserUniqueID UNIQUEIDENTIFIER,
    @GameID       INT,
    @Day          DATE,
    @Minutes      INT,
    @Platform     NVARCHAR (60),
    @Note         NVARCHAR (500)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);
    IF @UserID IS NULL
        RETURN;

    INSERT INTO [dbo].[PlaySession] ([UserID], [GameID], [Day], [Minutes], [Platform], [Note])
    VALUES (@UserID, @GameID, @Day, @Minutes, @Platform, @Note);

    SELECT CAST(SCOPE_IDENTITY() AS BIGINT) AS [SessionID];
END
