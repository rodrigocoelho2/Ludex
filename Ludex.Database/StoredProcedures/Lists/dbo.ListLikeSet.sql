/*
    ListLikeSet
    Likes a list the account may see, or takes the like back (@Like 0), and returns how many likes it has now. Nobody
    likes their own.
*/
CREATE PROCEDURE [dbo].[ListLikeSet]
    @UserUniqueID UNIQUEIDENTIFIER,
    @ListID       INT,
    @Like         BIT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);
    DECLARE @ViewerID BIGINT = @UserID;

    IF @UserID IS NOT NULL
    BEGIN
        IF @Like = 1
            INSERT INTO [dbo].[GameListLike] ([ListID], [UserID])
            SELECT l.[ListID], @UserID
            FROM [dbo].[GameList] l
            WHERE l.[ListID] = @ListID AND l.[UserID] <> @UserID AND (l.[UserID] = @ViewerID OR l.[Visibility] = 1 OR (l.[Visibility] = 2 AND EXISTS (SELECT 1 FROM [dbo].[Friendship] f WHERE f.[UserID] = @ViewerID AND f.[FriendUserID] = l.[UserID] AND f.[AcceptedAt] IS NOT NULL)))
              AND NOT EXISTS (SELECT 1 FROM [dbo].[GameListLike] k WHERE k.[ListID] = @ListID AND k.[UserID] = @UserID);
        ELSE
            DELETE FROM [dbo].[GameListLike]
            WHERE [ListID] = @ListID AND [UserID] = @UserID;
    END

    SELECT COUNT(*) AS [Likes] FROM [dbo].[GameListLike] WHERE [ListID] = @ListID;
END
