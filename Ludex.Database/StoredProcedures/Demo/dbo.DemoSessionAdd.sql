/*
    DemoSessionAdd
    A play session in a demo account's journal.
*/
CREATE PROCEDURE [dbo].[DemoSessionAdd]
    @UserUniqueID UNIQUEIDENTIFIER,
    @GameID       INT,
    @Day          DATE,
    @Minutes      INT,
    @Platform     NVARCHAR (60),
    @Note         NVARCHAR (500)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[PlaySession] ([UserID], [GameID], [Day], [Minutes], [Platform], [Note], [CreatedAt])
    SELECT u.[UserID], @GameID, @Day, @Minutes, @Platform, @Note, CAST(@Day AS DATETIME2 (3))
    FROM [dbo].[User] u
    WHERE u.[UserUniqueID] = @UserUniqueID AND u.[IsDemo] = 1;
END
