/*
    ReviewLikeSet
    Likes a review or takes the like back (@Like 0), and returns how many likes it has now. Nobody likes their own.
*/
CREATE PROCEDURE [dbo].[ReviewLikeSet]
    @UserUniqueID UNIQUEIDENTIFIER,
    @ReviewID     BIGINT,
    @Like         BIT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);

    IF @UserID IS NOT NULL
    BEGIN
        IF @Like = 1
            INSERT INTO [dbo].[ReviewLike] ([ReviewID], [UserID])
            SELECT r.[ReviewID], @UserID
            FROM [dbo].[Review] r
            WHERE r.[ReviewID] = @ReviewID AND r.[UserID] <> @UserID
              AND NOT EXISTS (SELECT 1 FROM [dbo].[ReviewLike] l WHERE l.[ReviewID] = @ReviewID AND l.[UserID] = @UserID);
        ELSE
            DELETE FROM [dbo].[ReviewLike]
            WHERE [ReviewID] = @ReviewID AND [UserID] = @UserID;
    END

    SELECT COUNT(*) AS [Likes] FROM [dbo].[ReviewLike] WHERE [ReviewID] = @ReviewID;
END
