/*
    DemoActivityAdd
    A line of a demo account's activity at a given time (the seed spreads them over the last weeks, so Trending has
    something to show).
*/
CREATE PROCEDURE [dbo].[DemoActivityAdd]
    @UserUniqueID UNIQUEIDENTIFIER,
    @GameID       INT,
    @Kind         TINYINT,
    @Rating       TINYINT,
    @At           DATETIME2 (3)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[Activity] ([UserID], [GameID], [Kind], [Rating], [At])
    SELECT u.[UserID], @GameID, @Kind, @Rating, @At
    FROM [dbo].[User] u
    WHERE u.[UserUniqueID] = @UserUniqueID AND u.[IsDemo] = 1;
END
