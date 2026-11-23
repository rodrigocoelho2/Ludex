/*
    UserList
    Every account, oldest first (not the made-up demo accounts: Settings shows those apart).
*/
CREATE PROCEDURE [dbo].[UserList]
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    SELECT u.[UserID],
           u.[UserUniqueID],
           u.[Username],
           u.[PasswordHash],
           u.[IsAdmin],
           u.[SecurityStamp],
           u.[CreatedAt],
           u.[LastLoginAt],
           u.[DiscordID],
           u.[DiscordName],
           p.[AvatarVersion],
           p.[LastSeenAt],
           p.[Accent]
    FROM [dbo].[User] u
    LEFT JOIN [dbo].[Profile] p ON p.[UserID] = u.[UserID]
    WHERE u.[IsDemo] = 0
    ORDER BY u.[UserID];
END
