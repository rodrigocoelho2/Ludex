CREATE TABLE [dbo].[Profile]
(
    [UserID]            BIGINT          NOT NULL,
    [Bio]               NVARCHAR (300)  NULL,
    [Location]          NVARCHAR (80)   NULL,
    [FavoriteGameID]    INT             NULL,
    [Avatar]            VARBINARY (MAX) NULL,
    [AvatarContentType] VARCHAR (40)    NULL,
    [AvatarVersion]     INT             NULL,
    [LastSeenAt]        DATETIME2 (3)   NULL,
    [SteamID]           VARCHAR (20)    NULL,
    [SteamSyncedAt]     DATETIME2 (3)   NULL,
    [Accent]            VARCHAR (20)    NULL,
    [Banner]            VARCHAR (10)    NULL,
    [BannerGameID]      INT             NULL,
    [Frame]             VARCHAR (10)    NULL,
    [Title]             VARCHAR (40)    NULL,
    [Tags]              NVARCHAR (200)  NULL,
    [FavoriteNote]      NVARCHAR (200)  NULL,
    [Showcases]         VARCHAR (200)   NULL,
    [PinnedCollectionID] INT            NULL,
    [LibraryVisibility] TINYINT         NULL,
    [ShowPlaying]       BIT             NULL,
    [UpdatedAt]         DATETIME2 (3)   NOT NULL CONSTRAINT [DF_Profile_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Profile] PRIMARY KEY CLUSTERED ([UserID] ASC),
    CONSTRAINT [FK_Profile_User] FOREIGN KEY ([UserID]) REFERENCES [dbo].[User] ([UserID]) ON DELETE CASCADE,
    CONSTRAINT [FK_Profile_Game] FOREIGN KEY ([FavoriteGameID]) REFERENCES [dbo].[Game] ([GameID]),
    CONSTRAINT [FK_Profile_BannerGame] FOREIGN KEY ([BannerGameID]) REFERENCES [dbo].[Game] ([GameID])
);
GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'What an account shows on its profile: bio, location, favourite game and photo (a small JPEG/PNG/WebP the Website already resized, at most 400 KB). AvatarVersion goes up with every new photo (it is in the photo''s address, so browsers keep each version forever); NULL = no photo. LastSeenAt is refreshed while the account uses Ludex (every couple of minutes). SteamID is the SteamID64 the library was last imported from (Sync again). Accent is the colour theme the account picked (Settings or Customize profile; NULL = the default cyan); the profile shows in it. Customize profile: Banner (art = a game''s artwork, BannerGameID or else the favourite; glow; plain; NULL = art), Frame round the photo (ring, glow, none; NULL = ring), Title (the id of an earned badge shown under the name), Tags (up to five, separated by |), FavoriteNote (a line about the favourite game), Showcases (the showcases shown, in order, comma separated; NULL = the default ones), PinnedCollectionID (a collection of the account''s, shown on the side), LibraryVisibility (1 friends, 2 everyone, 3 only me; NULL = friends) and ShowPlaying (NULL or 1 = others see what is being played). Created on first use; deleted with the account.',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'Profile';
