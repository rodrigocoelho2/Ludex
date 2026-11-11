/*
    FriendOwnership
    Which of an account's friends have each of the given games (a JSON array of game ids: [1942, 119133]).
    One row per friend and game.
*/
CREATE PROCEDURE [dbo].[FriendOwnership]
    @UserUniqueID UNIQUEIDENTIFIER,
    @GameIDs      NVARCHAR (MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);

    SELECT e.[GameID], u.[Username]
    FROM OPENJSON(@GameIDs) WITH ([GameID] INT '$') ids
    INNER JOIN [dbo].[LibraryEntry] e ON e.[GameID] = ids.[GameID]
    INNER JOIN [dbo].[Friendship] f ON f.[UserID] = @UserID AND f.[FriendUserID] = e.[UserID] AND f.[AcceptedAt] IS NOT NULL
    INNER JOIN [dbo].[User] u ON u.[UserID] = e.[UserID]
    ORDER BY e.[GameID], u.[Username];
END
