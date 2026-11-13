/*
    PsnAccountGet
    An account's PlayStation connection (no rows: never connected).
*/
CREATE PROCEDURE [dbo].[PsnAccountGet]
    @UserUniqueID UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    SELECT u.[UserUniqueID], a.[OnlineID], a.[Token], a.[TokenExpiresAt], a.[ConnectedAt], a.[SyncedAt], a.[WarnedAt]
    FROM [dbo].[PsnAccount] a
    INNER JOIN [dbo].[User] u ON u.[UserID] = a.[UserID]
    WHERE u.[UserUniqueID] = @UserUniqueID;
END
