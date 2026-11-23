/*
    ReviewCommentAdd
    Adds a comment under a review and returns its id (0 when the review is gone).
*/
CREATE PROCEDURE [dbo].[ReviewCommentAdd]
    @UserUniqueID UNIQUEIDENTIFIER,
    @ReviewID     BIGINT,
    @Text         NVARCHAR (1000),
    @At           DATETIME2 (3) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);
    DECLARE @CommentID BIGINT = 0;

    IF @UserID IS NOT NULL AND EXISTS (SELECT 1 FROM [dbo].[Review] WHERE [ReviewID] = @ReviewID)
    BEGIN
        INSERT INTO [dbo].[ReviewComment] ([ReviewID], [UserID], [Text], [At])
        VALUES (@ReviewID, @UserID, @Text, COALESCE(@At, SYSUTCDATETIME()));
        SET @CommentID = CAST(SCOPE_IDENTITY() AS BIGINT);
    END

    SELECT @CommentID AS [CommentID];
END
