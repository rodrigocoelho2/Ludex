/*
    GameUpsert
    Saves a game copied from the catalog (inserts it the first time, refreshes it afterwards) and returns it.
*/
CREATE PROCEDURE [dbo].[GameUpsert]
    @GameID           INT,
    @Name             NVARCHAR (300),
    @Slug             NVARCHAR (300),
    @Summary          NVARCHAR (4000),
    @CoverImageID     VARCHAR (64),
    @HeroImageID      VARCHAR (64),
    @Screenshots      VARCHAR (1000),
    @FirstReleaseDate DATE,
    @Developer        NVARCHAR (200),
    @Publisher        NVARCHAR (200),
    @Genres           NVARCHAR (400),
    @Platforms        NVARCHAR (1000),
    @Modes            NVARCHAR (400),
    @CriticScore      TINYINT = NULL,
    @TimeToBeatMain   DECIMAL (6, 1) = NULL,
    @TimeToBeatFull   DECIMAL (6, 1) = NULL,
    @DataVersion      TINYINT = 1
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    MERGE [dbo].[Game] WITH (HOLDLOCK) AS t
    USING (SELECT @GameID AS [GameID]) AS s
    ON t.[GameID] = s.[GameID]
    WHEN MATCHED THEN
        UPDATE SET [Name] = @Name,
                   [Slug] = @Slug,
                   [Summary] = @Summary,
                   [CoverImageID] = @CoverImageID,
                   [HeroImageID] = @HeroImageID,
                   [Screenshots] = @Screenshots,
                   [FirstReleaseDate] = @FirstReleaseDate,
                   [Developer] = @Developer,
                   [Publisher] = @Publisher,
                   [Genres] = @Genres,
                   [Platforms] = @Platforms,
                   [Modes] = @Modes,
                   [CriticScore] = @CriticScore,
                   [TimeToBeatMain] = @TimeToBeatMain,
                   [TimeToBeatFull] = @TimeToBeatFull,
                   [DataVersion] = @DataVersion,
                   [FetchedAt] = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT ([GameID], [Name], [Slug], [Summary], [CoverImageID], [HeroImageID], [Screenshots], [FirstReleaseDate],
                [Developer], [Publisher], [Genres], [Platforms], [Modes], [CriticScore], [TimeToBeatMain], [TimeToBeatFull], [DataVersion])
        VALUES (@GameID, @Name, @Slug, @Summary, @CoverImageID, @HeroImageID, @Screenshots, @FirstReleaseDate,
                @Developer, @Publisher, @Genres, @Platforms, @Modes, @CriticScore, @TimeToBeatMain, @TimeToBeatFull, @DataVersion);

    SELECT g.[GameID], g.[Name], g.[Slug], g.[Summary], g.[CoverImageID], g.[HeroImageID], g.[Screenshots],
           g.[FirstReleaseDate], g.[Developer], g.[Publisher], g.[Genres], g.[Platforms], g.[Modes], g.[FetchedAt],
           g.[CriticScore], g.[TimeToBeatMain], g.[TimeToBeatFull], g.[DataVersion]
    FROM [dbo].[Game] g
    WHERE g.[GameID] = @GameID;
END
