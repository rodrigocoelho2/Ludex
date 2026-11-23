/*
    ReviewGet
    One review with its author, its game, how many likes and comments it has, and whether the reader liked it.
*/
CREATE PROCEDURE [dbo].[ReviewGet]
    @ViewerUniqueID UNIQUEIDENTIFIER,
    @ReviewID       BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    DECLARE @ViewerID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @ViewerUniqueID);

    SELECT r.[ReviewID], r.[Rating], r.[Status], r.[Ending], r.[Platform], r.[Text], r.[Spoilers], r.[CreatedAt], r.[UpdatedAt],
           u.[UserUniqueID] AS [AuthorUniqueID], u.[Username] AS [AuthorName], u.[IsDemo] AS [AuthorIsDemo], p.[AvatarVersion] AS [AuthorAvatarVersion],
           (SELECT COUNT(*) FROM [dbo].[ReviewLike] l WHERE l.[ReviewID] = r.[ReviewID]) AS [Likes],
           (SELECT COUNT(*) FROM [dbo].[ReviewComment] c WHERE c.[ReviewID] = r.[ReviewID]) AS [Comments],
           CAST(CASE WHEN EXISTS (SELECT 1 FROM [dbo].[ReviewLike] ml WHERE ml.[ReviewID] = r.[ReviewID] AND ml.[UserID] = @ViewerID) THEN 1 ELSE 0 END AS BIT) AS [LikedByMe],
           CAST(CASE WHEN r.[UserID] = @ViewerID THEN 1 ELSE 0 END AS BIT) AS [IsMine],
           g.[GameID], g.[Name], g.[Slug], g.[CoverImageID], g.[HeroImageID], g.[FirstReleaseDate], g.[Developer], g.[Publisher], g.[Genres], g.[Platforms], g.[Modes], g.[FetchedAt], g.[CriticScore], g.[TimeToBeatMain], g.[TimeToBeatFull], g.[DataVersion], g.[Summary]
    FROM [dbo].[Review] r
    INNER JOIN [dbo].[User] u ON u.[UserID] = r.[UserID]
    LEFT JOIN [dbo].[Profile] p ON p.[UserID] = u.[UserID]
    INNER JOIN [dbo].[Game] g ON g.[GameID] = r.[GameID]
    WHERE r.[ReviewID] = @ReviewID;
END
