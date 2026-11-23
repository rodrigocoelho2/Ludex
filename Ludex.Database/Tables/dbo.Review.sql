CREATE TABLE [dbo].[Review]
(
    [ReviewID]  BIGINT          IDENTITY (1, 1) NOT NULL,
    [UserID]    BIGINT          NOT NULL,
    [GameID]    INT             NOT NULL,
    [Rating]    TINYINT         NULL,
    [Status]    TINYINT         NULL,
    [Ending]    TINYINT         NULL,
    [Platform]  NVARCHAR (60)   NULL,
    [Text]      NVARCHAR (4000) NOT NULL,
    [Spoilers]  BIT             NOT NULL CONSTRAINT [DF_Review_Spoilers] DEFAULT (0),
    [CreatedAt] DATETIME2 (3)   NOT NULL CONSTRAINT [DF_Review_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt] DATETIME2 (3)   NOT NULL CONSTRAINT [DF_Review_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Review] PRIMARY KEY CLUSTERED ([ReviewID] ASC),
    CONSTRAINT [UQ_Review_UserGame] UNIQUE NONCLUSTERED ([UserID] ASC, [GameID] ASC),
    CONSTRAINT [FK_Review_User] FOREIGN KEY ([UserID]) REFERENCES [dbo].[User] ([UserID]) ON DELETE CASCADE,
    CONSTRAINT [FK_Review_Game] FOREIGN KEY ([GameID]) REFERENCES [dbo].[Game] ([GameID]),
    CONSTRAINT [CK_Review_Rating] CHECK ([Rating] IS NULL OR [Rating] BETWEEN 1 AND 10)
);
GO

CREATE NONCLUSTERED INDEX [IX_Review_GameID]
    ON [dbo].[Review] ([GameID] ASC, [CreatedAt] DESC);
GO

CREATE NONCLUSTERED INDEX [IX_Review_CreatedAt]
    ON [dbo].[Review] ([CreatedAt] DESC)
    INCLUDE ([UserID], [GameID]);
GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'What an account wrote about a game (one review per account and game; writing again changes it). Rating, Status, Ending (see LibraryEntry) and Platform are copied from the log when the review is saved, so the review keeps saying "Finished on PC, 4½ stars" even if the library changes later. Every signed-in account on this Ludex can read reviews. Spoilers: the text is hidden until the reader asks for it.',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'Review';
