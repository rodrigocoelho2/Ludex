/*
    ListGet
    One list the reader may see (no row otherwise), with its owner, how many games and likes, and how many of its games
    the reader has played (finished, played or dropped).
*/
CREATE PROCEDURE [dbo].[ListGet]
    @ViewerUniqueID UNIQUEIDENTIFIER,
    @ListID         INT
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    DECLARE @ViewerID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @ViewerUniqueID);

    SELECT l.[ListID], l.[Name], l.[Description], l.[Ranked], l.[Visibility], l.[CreatedAt], l.[UpdatedAt],
           u.[UserUniqueID] AS [OwnerUniqueID], u.[Username] AS [OwnerName], u.[IsDemo] AS [OwnerIsDemo], p.[AvatarVersion] AS [OwnerAvatarVersion],
           (SELECT COUNT(*) FROM [dbo].[GameListItem] i WHERE i.[ListID] = l.[ListID]) AS [GameCount],
           (SELECT COUNT(*) FROM [dbo].[GameListLike] k WHERE k.[ListID] = l.[ListID]) AS [Likes],
           CAST(CASE WHEN EXISTS (SELECT 1 FROM [dbo].[GameListLike] mk WHERE mk.[ListID] = l.[ListID] AND mk.[UserID] = @ViewerID) THEN 1 ELSE 0 END AS BIT) AS [LikedByMe],
           CAST(CASE WHEN l.[UserID] = @ViewerID THEN 1 ELSE 0 END AS BIT) AS [IsMine],
           (SELECT COUNT(*) FROM [dbo].[GameListItem] pli
            WHERE pli.[ListID] = l.[ListID]
              AND EXISTS (SELECT 1 FROM [dbo].[LibraryEntry] pe WHERE pe.[UserID] = @ViewerID AND pe.[GameID] = pli.[GameID] AND pe.[Status] IN (2, 4, 6))) AS [PlayedByMe]
    FROM [dbo].[GameList] l
    INNER JOIN [dbo].[User] u ON u.[UserID] = l.[UserID]
    LEFT JOIN [dbo].[Profile] p ON p.[UserID] = u.[UserID]
    WHERE l.[ListID] = @ListID AND (l.[UserID] = @ViewerID OR l.[Visibility] = 1 OR (l.[Visibility] = 2 AND EXISTS (SELECT 1 FROM [dbo].[Friendship] f WHERE f.[UserID] = @ViewerID AND f.[FriendUserID] = l.[UserID] AND f.[AcceptedAt] IS NOT NULL)));
END
