/*
    SteamOwnedList
    The Steam apps an account already had at its last import or sync.
*/
CREATE PROCEDURE [dbo].[SteamOwnedList]
    @UserUniqueID UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    SELECT o.[AppID]
    FROM [dbo].[SteamOwned] o
    INNER JOIN [dbo].[User] u ON u.[UserID] = o.[UserID]
    WHERE u.[UserUniqueID] = @UserUniqueID;
END
