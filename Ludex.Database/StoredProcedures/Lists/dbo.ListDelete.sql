/*
    ListDelete
    Deletes one of the account's own lists (its games and likes go with it).
*/
CREATE PROCEDURE [dbo].[ListDelete]
    @UserUniqueID UNIQUEIDENTIFIER,
    @ListID       INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);

    DELETE FROM [dbo].[GameList]
    WHERE [ListID] = @ListID AND [UserID] = @UserID;
END
