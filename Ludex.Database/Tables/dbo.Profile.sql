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
    [UpdatedAt]         DATETIME2 (3)   NOT NULL CONSTRAINT [DF_Profile_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Profile] PRIMARY KEY CLUSTERED ([UserID] ASC),
    CONSTRAINT [FK_Profile_User] FOREIGN KEY ([UserID]) REFERENCES [dbo].[User] ([UserID]) ON DELETE CASCADE,
    CONSTRAINT [FK_Profile_Game] FOREIGN KEY ([FavoriteGameID]) REFERENCES [dbo].[Game] ([GameID])
);
GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'What an account shows on its profile: bio, location, favourite game and photo (a small JPEG/PNG/WebP the Website already resized, at most 400 KB). AvatarVersion goes up with every new photo (it is in the photo''s address, so browsers keep each version forever); NULL = no photo. LastSeenAt is refreshed while the account uses Ludex (every couple of minutes). Created on first use; deleted with the account.',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'Profile';
