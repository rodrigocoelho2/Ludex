/*
    ProfileAvatarGet
    The profile photo and its content type (no row when there is none).
*/
CREATE PROCEDURE [dbo].[ProfileAvatarGet]
    @UserUniqueID UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    SELECT p.[Avatar], p.[AvatarContentType], p.[AvatarVersion]
    FROM [dbo].[User] u
    INNER JOIN [dbo].[Profile] p ON p.[UserID] = u.[UserID]
    WHERE u.[UserUniqueID] = @UserUniqueID AND p.[Avatar] IS NOT NULL;
END
