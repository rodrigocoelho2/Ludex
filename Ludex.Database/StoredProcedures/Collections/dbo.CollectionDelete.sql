/*
    CollectionDelete
    Deletes one of an account's collections (its games stay in the library).
*/
CREATE PROCEDURE [dbo].[CollectionDelete]
    @UserUniqueID UNIQUEIDENTIFIER,
    @CollectionID INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);

    DELETE FROM [dbo].[Collection] WHERE [CollectionID] = @CollectionID AND [UserID] = @UserID;
END
