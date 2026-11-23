/*
    ReviewCommentList
    The comments under a review, oldest first, and whether the reader may delete each (theirs, or on their review).
*/
CREATE PROCEDURE [dbo].[ReviewCommentList]
    @ViewerUniqueID UNIQUEIDENTIFIER,
    @ReviewID       BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    DECLARE @ViewerID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @ViewerUniqueID);

    SELECT c.[CommentID], c.[Text], c.[At],
           u.[UserUniqueID], u.[Username], u.[IsDemo], p.[AvatarVersion],
           CAST(CASE WHEN c.[UserID] = @ViewerID OR r.[UserID] = @ViewerID THEN 1 ELSE 0 END AS BIT) AS [CanDelete]
    FROM [dbo].[ReviewComment] c
    INNER JOIN [dbo].[Review] r ON r.[ReviewID] = c.[ReviewID]
    INNER JOIN [dbo].[User] u ON u.[UserID] = c.[UserID]
    LEFT JOIN [dbo].[Profile] p ON p.[UserID] = u.[UserID]
    WHERE c.[ReviewID] = @ReviewID
    ORDER BY c.[At], c.[CommentID];
END
