/*
    CollectionSave
    Creates a collection (@CollectionID null) or renames one. Returns its id; 0 when it isn't the account's; -1 when
    the account already has a collection with that name.
*/
CREATE PROCEDURE [dbo].[CollectionSave]
    @UserUniqueID UNIQUEIDENTIFIER,
    @CollectionID INT,
    @Name         NVARCHAR (60)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);
    IF @UserID IS NULL
    BEGIN
        SELECT 0;
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM [dbo].[Collection] WHERE [UserID] = @UserID AND [Name] = @Name AND [CollectionID] <> ISNULL(@CollectionID, 0))
    BEGIN
        SELECT -1;
        RETURN;
    END

    IF @CollectionID IS NULL
    BEGIN
        INSERT INTO [dbo].[Collection] ([UserID], [Name]) VALUES (@UserID, @Name);
        SELECT CAST(SCOPE_IDENTITY() AS INT);
        RETURN;
    END

    UPDATE [dbo].[Collection] SET [Name] = @Name WHERE [CollectionID] = @CollectionID AND [UserID] = @UserID;
    SELECT CASE WHEN @@ROWCOUNT = 0 THEN 0 ELSE @CollectionID END;
END
