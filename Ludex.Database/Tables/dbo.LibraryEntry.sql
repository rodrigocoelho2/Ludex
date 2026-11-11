CREATE TABLE [dbo].[LibraryEntry]
(
    [UserID]     BIGINT          NOT NULL,
    [GameID]     INT             NOT NULL,
    [Status]     TINYINT         NOT NULL,
    [Rating]     TINYINT         NULL,
    [Hours]      DECIMAL (7, 1)  NULL,
    [Platform]   NVARCHAR (60)   NULL,
    [Notes]      NVARCHAR (2000) NULL,
    [AddedAt]    DATETIME2 (3)   NOT NULL CONSTRAINT [DF_LibraryEntry_AddedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]  DATETIME2 (3)   NOT NULL CONSTRAINT [DF_LibraryEntry_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    [FinishedAt] DATETIME2 (3)   NULL,
    CONSTRAINT [PK_LibraryEntry] PRIMARY KEY CLUSTERED ([UserID] ASC, [GameID] ASC),
    CONSTRAINT [FK_LibraryEntry_User] FOREIGN KEY ([UserID]) REFERENCES [dbo].[User] ([UserID]) ON DELETE CASCADE,
    CONSTRAINT [FK_LibraryEntry_Game] FOREIGN KEY ([GameID]) REFERENCES [dbo].[Game] ([GameID]),
    CONSTRAINT [CK_LibraryEntry_Status] CHECK ([Status] BETWEEN 1 AND 4),
    CONSTRAINT [CK_LibraryEntry_Rating] CHECK ([Rating] IS NULL OR [Rating] BETWEEN 1 AND 10),
    CONSTRAINT [CK_LibraryEntry_Hours] CHECK ([Hours] IS NULL OR [Hours] >= 0)
);
GO

CREATE NONCLUSTERED INDEX [IX_LibraryEntry_GameID]
    ON [dbo].[LibraryEntry] ([GameID] ASC)
    INCLUDE ([Status], [Rating]);
GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'One game in one account''s library. Status: 1 Playing, 2 Finished, 3 Wishlist, 4 Dropped. Rating 1–10. FinishedAt is set the first time the status becomes Finished.',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'LibraryEntry';
