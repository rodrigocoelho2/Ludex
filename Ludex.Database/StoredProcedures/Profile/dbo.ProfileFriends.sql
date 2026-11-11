/*
    ProfileFriends
    An account's friends for its profile: photo, when they were last seen, the game they're playing (the Playing game
    changed last) and how many games they have.
*/
CREATE PROCEDURE [dbo].[ProfileFriends]
    @UserUniqueID UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);

    SELECT u.[UserUniqueID],
           u.[Username],
           p.[AvatarVersion],
           CASE WHEN p.[LastSeenAt] IS NULL OR u.[LastLoginAt] > p.[LastSeenAt] THEN u.[LastLoginAt] ELSE p.[LastSeenAt] END AS [LastSeenAt],
           playing.[GameID] AS [PlayingGameID],
           playing.[Name] AS [PlayingName],
           (SELECT COUNT(*) FROM [dbo].[LibraryEntry] e WHERE e.[UserID] = u.[UserID]) AS [GameCount]
    FROM [dbo].[Friendship] f
    INNER JOIN [dbo].[User] u ON u.[UserID] = f.[FriendUserID]
    LEFT JOIN [dbo].[Profile] p ON p.[UserID] = u.[UserID]
    OUTER APPLY (SELECT TOP (1) g.[GameID], g.[Name]
                 FROM [dbo].[LibraryEntry] e
                 INNER JOIN [dbo].[Game] g ON g.[GameID] = e.[GameID]
                 WHERE e.[UserID] = u.[UserID] AND e.[Status] = 1
                 ORDER BY e.[UpdatedAt] DESC) playing
    WHERE f.[UserID] = @UserID AND f.[AcceptedAt] IS NOT NULL
    ORDER BY CASE WHEN playing.[GameID] IS NULL THEN 1 ELSE 0 END, CASE WHEN p.[LastSeenAt] IS NULL OR u.[LastLoginAt] > p.[LastSeenAt] THEN u.[LastLoginAt] ELSE p.[LastSeenAt] END DESC, u.[Username];
END
