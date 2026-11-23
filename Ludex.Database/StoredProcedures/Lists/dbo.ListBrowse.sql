/*
    ListBrowse
    Lists the reader may see: one account's (@OwnerUniqueID), the ones holding a game (@GameID), or everyone's.
    @Sort: 1 popular (likes in the last @Days days, then all likes), 2 recently changed. Lists without games are left
    out unless they are the reader's own.
*/
CREATE PROCEDURE [dbo].[ListBrowse]
    @ViewerUniqueID UNIQUEIDENTIFIER,
    @OwnerUniqueID  UNIQUEIDENTIFIER = NULL,
    @GameID         INT = NULL,
    @Sort           TINYINT = 1,
    @Days           INT = 7,
    @Take           INT = 20
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    DECLARE @ViewerID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @ViewerUniqueID);
    DECLARE @OwnerID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @OwnerUniqueID);
    DECLARE @Since DATETIME2 (3) = DATEADD(DAY, -@Days, SYSUTCDATETIME());

    SELECT TOP (@Take) l.[ListID], l.[Name], l.[Description], l.[Ranked], l.[Visibility], l.[CreatedAt], l.[UpdatedAt],
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
    OUTER APPLY (SELECT COUNT(*) AS [Recent] FROM [dbo].[GameListLike] rk WHERE rk.[ListID] = l.[ListID] AND rk.[At] >= @Since) recent
    OUTER APPLY (SELECT COUNT(*) AS [All] FROM [dbo].[GameListLike] ak WHERE ak.[ListID] = l.[ListID]) total
    WHERE (l.[UserID] = @ViewerID OR l.[Visibility] = 1 OR (l.[Visibility] = 2 AND EXISTS (SELECT 1 FROM [dbo].[Friendship] f WHERE f.[UserID] = @ViewerID AND f.[FriendUserID] = l.[UserID] AND f.[AcceptedAt] IS NOT NULL)))
      AND (@OwnerUniqueID IS NULL OR l.[UserID] = @OwnerID)
      AND (@GameID IS NULL OR EXISTS (SELECT 1 FROM [dbo].[GameListItem] gi WHERE gi.[ListID] = l.[ListID] AND gi.[GameID] = @GameID))
      AND (l.[UserID] = @ViewerID OR EXISTS (SELECT 1 FROM [dbo].[GameListItem] ni WHERE ni.[ListID] = l.[ListID]))
    ORDER BY
        CASE WHEN @Sort = 1 THEN recent.[Recent] END DESC,
        CASE WHEN @Sort = 1 THEN total.[All] END DESC,
        l.[UpdatedAt] DESC,
        l.[ListID] DESC;
END
