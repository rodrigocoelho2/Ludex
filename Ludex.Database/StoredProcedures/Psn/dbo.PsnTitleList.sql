/*
    PsnTitleList
    The PlayStation titles an account has seen, with the game each one is (NULL: no match).
*/
CREATE PROCEDURE [dbo].[PsnTitleList]
    @UserUniqueID UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    SELECT t.[TitleKey], t.[Name], t.[GameID]
    FROM [dbo].[PsnTitle] t
    INNER JOIN [dbo].[User] u ON u.[UserID] = t.[UserID]
    WHERE u.[UserUniqueID] = @UserUniqueID;
END
