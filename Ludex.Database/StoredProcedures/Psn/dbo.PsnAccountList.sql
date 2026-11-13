/*
    PsnAccountList
    The PlayStation connections to sync (with a token), the least recently synced first.
*/
CREATE PROCEDURE [dbo].[PsnAccountList]
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    SELECT u.[UserUniqueID], a.[OnlineID], a.[Token], a.[TokenExpiresAt], a.[ConnectedAt], a.[SyncedAt], a.[WarnedAt]
    FROM [dbo].[PsnAccount] a
    INNER JOIN [dbo].[User] u ON u.[UserID] = a.[UserID]
    WHERE a.[Token] IS NOT NULL
    ORDER BY a.[SyncedAt];
END
