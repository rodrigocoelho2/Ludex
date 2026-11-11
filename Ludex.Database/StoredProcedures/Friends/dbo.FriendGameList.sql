/*
    FriendGameList
    The account's friends who have a game, with where they are in it. Only accepted friends: a pending request sees nothing.
*/
CREATE PROCEDURE [dbo].[FriendGameList]
    @UserUniqueID UNIQUEIDENTIFIER,
    @GameID       INT
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);

    SELECT u.[UserUniqueID], u.[Username], p.[AvatarVersion], e.[Status], e.[Rating], e.[Hours], e.[Platform], e.[UpdatedAt]
    FROM [dbo].[Friendship] f
    INNER JOIN [dbo].[LibraryEntry] e ON e.[UserID] = f.[FriendUserID] AND e.[GameID] = @GameID
    INNER JOIN [dbo].[User] u ON u.[UserID] = f.[FriendUserID]
    LEFT JOIN [dbo].[Profile] p ON p.[UserID] = u.[UserID]
    WHERE f.[UserID] = @UserID AND f.[AcceptedAt] IS NOT NULL
    ORDER BY e.[UpdatedAt] DESC;
END
