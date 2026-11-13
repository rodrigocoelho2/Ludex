/*
    ProfileCustomize
    Saves everything Customize profile sets in one go: bio, location, favourite game and its line, colour, banner,
    frame, title, tags, showcases, pinned collection and who sees the library and what is being played. The WebApi
    checks the values; games that aren't in dbo.Game are dropped here.
*/
CREATE PROCEDURE [dbo].[ProfileCustomize]
    @UserUniqueID       UNIQUEIDENTIFIER,
    @Bio                NVARCHAR (300),
    @Location           NVARCHAR (80),
    @FavoriteGameID     INT,
    @FavoriteNote       NVARCHAR (200),
    @Accent             VARCHAR (20),
    @Banner             VARCHAR (10),
    @BannerGameID       INT,
    @Frame              VARCHAR (10),
    @Title              VARCHAR (40),
    @Tags               NVARCHAR (200),
    @Showcases          VARCHAR (200),
    @PinnedCollectionID INT,
    @LibraryVisibility  TINYINT,
    @ShowPlaying        BIT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);
    IF @UserID IS NULL
        RETURN;

    IF @FavoriteGameID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Game] WHERE [GameID] = @FavoriteGameID)
        SET @FavoriteGameID = NULL;
    IF @BannerGameID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Game] WHERE [GameID] = @BannerGameID)
        SET @BannerGameID = NULL;
    IF @PinnedCollectionID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Collection] WHERE [CollectionID] = @PinnedCollectionID AND [UserID] = @UserID)
        SET @PinnedCollectionID = NULL;

    MERGE [dbo].[Profile] WITH (HOLDLOCK) AS t
    USING (SELECT @UserID AS [UserID]) AS s
    ON t.[UserID] = s.[UserID]
    WHEN MATCHED THEN
        UPDATE SET [Bio] = @Bio, [Location] = @Location, [FavoriteGameID] = @FavoriteGameID, [FavoriteNote] = @FavoriteNote,
                   [Accent] = @Accent, [Banner] = @Banner, [BannerGameID] = @BannerGameID, [Frame] = @Frame, [Title] = @Title,
                   [Tags] = @Tags, [Showcases] = @Showcases, [PinnedCollectionID] = @PinnedCollectionID,
                   [LibraryVisibility] = @LibraryVisibility, [ShowPlaying] = @ShowPlaying, [UpdatedAt] = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT ([UserID], [Bio], [Location], [FavoriteGameID], [FavoriteNote], [Accent], [Banner], [BannerGameID], [Frame], [Title],
                [Tags], [Showcases], [PinnedCollectionID], [LibraryVisibility], [ShowPlaying])
        VALUES (@UserID, @Bio, @Location, @FavoriteGameID, @FavoriteNote, @Accent, @Banner, @BannerGameID, @Frame, @Title,
                @Tags, @Showcases, @PinnedCollectionID, @LibraryVisibility, @ShowPlaying);
END
