/*
    CollectionList
    An account's collections, by name, with the games in each (one row per collection and game; GameID null for an
    empty collection). Only games still in the library are listed.
*/
CREATE PROCEDURE [dbo].[CollectionList]
    @UserUniqueID UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);

    SELECT DISTINCT c.[CollectionID], c.[Name], e.[GameID]
    FROM [dbo].[Collection] c
    LEFT JOIN [dbo].[CollectionGame] cg ON cg.[CollectionID] = c.[CollectionID]
    LEFT JOIN [dbo].[LibraryEntry] e ON e.[UserID] = @UserID AND e.[GameID] = cg.[GameID]
    WHERE c.[UserID] = @UserID
    ORDER BY c.[Name], c.[CollectionID];
END
