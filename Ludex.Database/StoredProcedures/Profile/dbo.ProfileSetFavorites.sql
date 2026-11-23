/*
    ProfileSetFavorites
    The four favourite games at the top of a profile (game ids, comma separated, in order; NULL: none). Ids of games
    that aren't in dbo.Game are dropped by the WebApi before it gets here.
*/
CREATE PROCEDURE [dbo].[ProfileSetFavorites]
    @UserUniqueID UNIQUEIDENTIFIER,
    @Favorites    VARCHAR (60)
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
        UPDATE SET [Favorites] = @Favorites, [UpdatedAt] = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT ([UserID], [Favorites]) VALUES (@UserID, @Favorites);
END
