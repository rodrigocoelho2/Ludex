/*
    ReviewLikers
    Who liked a review, newest first (the names in "Liked by …").
*/
CREATE PROCEDURE [dbo].[ReviewLikers]
    @ReviewID BIGINT,
    @Take     INT
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    SELECT TOP (@Take) u.[Username]
    FROM [dbo].[ReviewLike] l
    INNER JOIN [dbo].[User] u ON u.[UserID] = l.[UserID]
    WHERE l.[ReviewID] = @ReviewID
    ORDER BY l.[At] DESC;
END
