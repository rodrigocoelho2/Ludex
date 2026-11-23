/*
    ReviewList
    Reviews, for a game (@GameID), by an account (@AuthorUniqueID), or of everyone, a page at a time (@Skip, @Take).
    @Sort: 1 popular now (likes in the last @Days days, then all likes, then newest), 2 newest, 3 the reader's friends
    only (newest), 4 highest rated, 5 popular of all time. Every signed-in account can read every review.
*/
CREATE PROCEDURE [dbo].[ReviewList]
    @ViewerUniqueID UNIQUEIDENTIFIER,
    @GameID         INT = NULL,
    @AuthorUniqueID UNIQUEIDENTIFIER = NULL,
    @Sort           TINYINT = 1,
    @Days           INT = 7,
    @Skip           INT = 0,
    @Take           INT = 20
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    DECLARE @ViewerID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @ViewerUniqueID);
    DECLARE @AuthorID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @AuthorUniqueID);
    DECLARE @Since DATETIME2 (3) = DATEADD(DAY, -@Days, SYSUTCDATETIME());

    SELECT r.[ReviewID], r.[Rating], r.[Status], r.[Ending], r.[Platform], r.[Text], r.[Spoilers], r.[CreatedAt], r.[UpdatedAt],
           u.[UserUniqueID] AS [AuthorUniqueID], u.[Username] AS [AuthorName], u.[IsDemo] AS [AuthorIsDemo], p.[AvatarVersion] AS [AuthorAvatarVersion],
           (SELECT COUNT(*) FROM [dbo].[ReviewLike] l WHERE l.[ReviewID] = r.[ReviewID]) AS [Likes],
           (SELECT COUNT(*) FROM [dbo].[ReviewComment] c WHERE c.[ReviewID] = r.[ReviewID]) AS [Comments],
           CAST(CASE WHEN EXISTS (SELECT 1 FROM [dbo].[ReviewLike] ml WHERE ml.[ReviewID] = r.[ReviewID] AND ml.[UserID] = @ViewerID) THEN 1 ELSE 0 END AS BIT) AS [LikedByMe],
           CAST(CASE WHEN r.[UserID] = @ViewerID THEN 1 ELSE 0 END AS BIT) AS [IsMine],
           g.[GameID], g.[Name], g.[Slug], g.[CoverImageID], g.[HeroImageID], g.[FirstReleaseDate], g.[Developer], g.[Publisher], g.[Genres], g.[Platforms], g.[Modes], g.[FetchedAt], g.[CriticScore], g.[TimeToBeatMain], g.[TimeToBeatFull], g.[DataVersion]
    FROM [dbo].[Review] r
    INNER JOIN [dbo].[User] u ON u.[UserID] = r.[UserID]
    LEFT JOIN [dbo].[Profile] p ON p.[UserID] = u.[UserID]
    INNER JOIN [dbo].[Game] g ON g.[GameID] = r.[GameID]
    OUTER APPLY (SELECT COUNT(*) AS [Recent] FROM [dbo].[ReviewLike] rl WHERE rl.[ReviewID] = r.[ReviewID] AND rl.[At] >= @Since) recent
    OUTER APPLY (SELECT COUNT(*) AS [All] FROM [dbo].[ReviewLike] al WHERE al.[ReviewID] = r.[ReviewID]) total
    WHERE (@GameID IS NULL OR r.[GameID] = @GameID)
      AND (@AuthorUniqueID IS NULL OR r.[UserID] = @AuthorID)
      AND (@Sort <> 3 OR EXISTS (SELECT 1 FROM [dbo].[Friendship] f WHERE f.[UserID] = @ViewerID AND f.[FriendUserID] = r.[UserID] AND f.[AcceptedAt] IS NOT NULL))
    ORDER BY
        CASE WHEN @Sort = 1 THEN recent.[Recent] END DESC,
        CASE WHEN @Sort IN (1, 5) THEN total.[All] END DESC,
        CASE WHEN @Sort = 4 THEN r.[Rating] END DESC,
        r.[CreatedAt] DESC,
        r.[ReviewID] DESC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END
