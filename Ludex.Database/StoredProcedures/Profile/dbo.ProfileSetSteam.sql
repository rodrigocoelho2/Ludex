/*
    ProfileSetSteam
    Remembers the Steam account (SteamID64) a library was imported from, so "Sync again" needs nothing typed.
*/
CREATE PROCEDURE [dbo].[ProfileSetSteam]
    @UserUniqueID UNIQUEIDENTIFIER,
    @SteamID      VARCHAR (20)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);
    IF @UserID IS NULL
        RETURN;

    MERGE [dbo].[Profile] WITH (HOLDLOCK) AS t
    USING (SELECT @UserID AS [UserID]) AS s
    ON t.[UserID] = s.[UserID]
    WHEN MATCHED THEN
        UPDATE SET [SteamID] = @SteamID, [UpdatedAt] = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT ([UserID], [SteamID]) VALUES (@UserID, @SteamID);
END
