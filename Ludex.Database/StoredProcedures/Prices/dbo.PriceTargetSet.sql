/*
    PriceTargetSet
    Sets the price under which a wishlisted game raises an alert (null: no alert). Re-arms the alert.
*/
CREATE PROCEDURE [dbo].[PriceTargetSet]
    @UserUniqueID UNIQUEIDENTIFIER,
    @GameID       INT,
    @Target       DECIMAL (9, 2)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);

    UPDATE [dbo].[LibraryEntry] SET [PriceTarget] = @Target, [PriceAlerted] = NULL
    WHERE [UserID] = @UserID AND [GameID] = @GameID;
END
