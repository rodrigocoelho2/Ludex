/*
    ReviewCommentDelete
    Deletes a comment: its writer can, and so can the review's author.
*/
CREATE PROCEDURE [dbo].[ReviewCommentDelete]
    @UserUniqueID UNIQUEIDENTIFIER,
    @CommentID    BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);

    DELETE c
    FROM [dbo].[ReviewComment] c
    INNER JOIN [dbo].[Review] r ON r.[ReviewID] = c.[ReviewID]
    WHERE c.[CommentID] = @CommentID AND (c.[UserID] = @UserID OR r.[UserID] = @UserID);
END
