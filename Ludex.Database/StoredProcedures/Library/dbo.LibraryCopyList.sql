/*
    LibraryCopyList
    The entries (copies) of an account's games: one game (@GameID), or every game (NULL). The imports use it to pick
    the copy a game goes into (the same platform family), and the game page to list a game's copies.
*/
CREATE PROCEDURE [dbo].[LibraryCopyList]
    @UserUniqueID UNIQUEIDENTIFIER,
    @GameID       INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);

    SELECT e.[EntryID], e.[GameID], e.[Status], e.[Rating], e.[Hours], e.[Platform], e.[Store], e.[Format], e.[SteamAppID],
           e.[AchievementsUnlocked], e.[AchievementsTotal], e.[UpdatedAt]
    FROM [dbo].[LibraryEntry] e
    WHERE e.[UserID] = @UserID AND (@GameID IS NULL OR e.[GameID] = @GameID)
    ORDER BY e.[GameID], e.[EntryID];
END
