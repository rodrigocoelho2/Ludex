/*
    SessionDelete
    Takes one of an account's own sessions out of the journal.
*/
CREATE PROCEDURE [dbo].[SessionDelete]
    @UserUniqueID UNIQUEIDENTIFIER,
    @SessionID    BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);

    DELETE FROM [dbo].[PlaySession]
    WHERE [SessionID] = @SessionID AND [UserID] = @UserID;
END
