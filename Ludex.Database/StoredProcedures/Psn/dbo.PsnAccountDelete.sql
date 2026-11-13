/*
    PsnAccountDelete
    Disconnects PlayStation: forgets the connection and the titles seen. The games already imported stay.
*/
CREATE PROCEDURE [dbo].[PsnAccountDelete]
    @UserUniqueID UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);
    IF @UserID IS NULL
        RETURN;

    BEGIN TRANSACTION;
    DELETE FROM [dbo].[PsnTitle] WHERE [UserID] = @UserID;
    DELETE FROM [dbo].[PsnAccount] WHERE [UserID] = @UserID;
    COMMIT TRANSACTION;
END
