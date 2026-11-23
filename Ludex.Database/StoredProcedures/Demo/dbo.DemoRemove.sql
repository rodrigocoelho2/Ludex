/*
    DemoRemove
    Deletes every made-up demo account and everything it wrote: reviews (with the likes and comments real accounts left
    on them), likes, comments, lists, journal, library, activity and friendships. Returns how many accounts went.
*/
CREATE PROCEDURE [dbo].[DemoRemove]
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Demo TABLE ([UserID] BIGINT PRIMARY KEY);
    INSERT INTO @Demo ([UserID]) SELECT [UserID] FROM [dbo].[User] WHERE [IsDemo] = 1;

    BEGIN TRANSACTION;

    DELETE FROM [dbo].[ReviewLike] WHERE [UserID] IN (SELECT [UserID] FROM @Demo);
    DELETE FROM [dbo].[ReviewComment] WHERE [UserID] IN (SELECT [UserID] FROM @Demo);
    DELETE FROM [dbo].[GameListLike] WHERE [UserID] IN (SELECT [UserID] FROM @Demo);
    DELETE FROM [dbo].[Friendship] WHERE [UserID] IN (SELECT [UserID] FROM @Demo) OR [FriendUserID] IN (SELECT [UserID] FROM @Demo);
    DELETE FROM [dbo].[User] WHERE [UserID] IN (SELECT [UserID] FROM @Demo);

    COMMIT TRANSACTION;

    SELECT COUNT(*) AS [Removed] FROM @Demo;
END
