/*
    ProfileGet
    An account's profile (without the photo bytes). Every account has one: missing values come back NULL.
*/
CREATE PROCEDURE [dbo].[ProfileGet]
    @UserUniqueID UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    SELECT u.[UserUniqueID],
           u.[Username],
           u.[CreatedAt],
           u.[LastLoginAt],
           p.[Bio],
           p.[Location],
           p.[FavoriteGameID],
           p.[AvatarVersion],
           p.[LastSeenAt],
           p.[SteamID]
    FROM [dbo].[User] u
    LEFT JOIN [dbo].[Profile] p ON p.[UserID] = u.[UserID]
    WHERE u.[UserUniqueID] = @UserUniqueID;
END
