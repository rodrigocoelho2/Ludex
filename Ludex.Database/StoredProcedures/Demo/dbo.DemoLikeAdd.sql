/*
    DemoLikeAdd
    A demo account likes a review or a list at a given time (@ReviewID or @ListID).
*/
CREATE PROCEDURE [dbo].[DemoLikeAdd]
    @UserUniqueID UNIQUEIDENTIFIER,
    @ReviewID     BIGINT = NULL,
    @ListID       INT = NULL,
    @At           DATETIME2 (3)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID AND [IsDemo] = 1);
    IF @UserID IS NULL
        RETURN;

    IF @ReviewID IS NOT NULL
        INSERT INTO [dbo].[ReviewLike] ([ReviewID], [UserID], [At])
        SELECT r.[ReviewID], @UserID, @At FROM [dbo].[Review] r
        WHERE r.[ReviewID] = @ReviewID AND r.[UserID] <> @UserID
          AND NOT EXISTS (SELECT 1 FROM [dbo].[ReviewLike] l WHERE l.[ReviewID] = @ReviewID AND l.[UserID] = @UserID);

    IF @ListID IS NOT NULL
        INSERT INTO [dbo].[GameListLike] ([ListID], [UserID], [At])
        SELECT l.[ListID], @UserID, @At FROM [dbo].[GameList] l
        WHERE l.[ListID] = @ListID AND l.[UserID] <> @UserID
          AND NOT EXISTS (SELECT 1 FROM [dbo].[GameListLike] k WHERE k.[ListID] = @ListID AND k.[UserID] = @UserID);
END
