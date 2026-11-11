CREATE TABLE [dbo].[Game]
(
    [GameID]           INT             NOT NULL,
    [Name]             NVARCHAR (300)  NOT NULL,
    [Slug]             NVARCHAR (300)  NULL,
    [Summary]          NVARCHAR (4000) NULL,
    [CoverImageID]     VARCHAR (64)    NULL,
    [HeroImageID]      VARCHAR (64)    NULL,
    [Screenshots]      VARCHAR (1000)  NULL,
    [FirstReleaseDate] DATE            NULL,
    [Developer]        NVARCHAR (200)  NULL,
    [Publisher]        NVARCHAR (200)  NULL,
    [Genres]           NVARCHAR (400)  NULL,
    [Platforms]        NVARCHAR (1000) NULL,
    [Modes]            NVARCHAR (400)  NULL,
    [FetchedAt]        DATETIME2 (3)   NOT NULL CONSTRAINT [DF_Game_FetchedAt] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Game] PRIMARY KEY CLUSTERED ([GameID] ASC)
);
GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'The games anyone has added or opened, copied from the catalog (IGDB, or the list built into Ludex). GameID is the IGDB id; the built-in list uses ids from 1,000,000,000. Screenshots is a comma-separated list of IGDB image ids; Genres, Platforms and Modes are separated by |.',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'Game';
