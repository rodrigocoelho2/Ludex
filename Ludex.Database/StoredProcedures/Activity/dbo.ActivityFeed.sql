/*
    ActivityFeed
    What an account's friends did lately, newest first, with whether the account has the same game (MyStatus).
*/
CREATE PROCEDURE [dbo].[ActivityFeed]
    @UserUniqueID UNIQUEIDENTIFIER,
    @Take         INT
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);

    SELECT TOP (@Take)
           a.[ActivityID], a.[Kind], a.[Rating], a.[At],
           u.[UserUniqueID], u.[Username], p.[AvatarVersion],
           g.[GameID], g.[Name], g.[CoverImageID],
           mine.[Status] AS [MyStatus]
    FROM [dbo].[Friendship] f
    INNER JOIN [dbo].[Activity] a ON a.[UserID] = f.[FriendUserID]
    INNER JOIN [dbo].[User] u ON u.[UserID] = a.[UserID]
    LEFT JOIN [dbo].[Profile] p ON p.[UserID] = u.[UserID]
    INNER JOIN [dbo].[Game] g ON g.[GameID] = a.[GameID]
    LEFT JOIN [dbo].[LibraryEntry] mine ON mine.[UserID] = @UserID AND mine.[GameID] = a.[GameID]
    WHERE f.[UserID] = @UserID AND f.[AcceptedAt] IS NOT NULL
    ORDER BY a.[At] DESC, a.[ActivityID] DESC;
END
