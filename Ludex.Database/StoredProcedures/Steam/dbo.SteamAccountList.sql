/*
    SteamAccountList
    The accounts with a Steam library to sync (imported once), the least recently synced first.
*/
CREATE PROCEDURE [dbo].[SteamAccountList]
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    SELECT u.[UserUniqueID], p.[SteamID], p.[SteamSyncedAt]
    FROM [dbo].[Profile] p
    INNER JOIN [dbo].[User] u ON u.[UserID] = p.[UserID]
    WHERE p.[SteamID] IS NOT NULL
    ORDER BY p.[SteamSyncedAt];
END
