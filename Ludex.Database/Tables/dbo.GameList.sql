CREATE TABLE [dbo].[GameList]
(
    [ListID]      INT             IDENTITY (1, 1) NOT NULL,
    [UserID]      BIGINT          NOT NULL,
    [Name]        NVARCHAR (80)   NOT NULL,
    [Description] NVARCHAR (1000) NULL,
    [Ranked]      BIT             NOT NULL CONSTRAINT [DF_GameList_Ranked] DEFAULT (0),
    [Visibility]  TINYINT         NOT NULL CONSTRAINT [DF_GameList_Visibility] DEFAULT (1),
    [CreatedAt]   DATETIME2 (3)   NOT NULL CONSTRAINT [DF_GameList_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [UpdatedAt]   DATETIME2 (3)   NOT NULL CONSTRAINT [DF_GameList_UpdatedAt] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_GameList] PRIMARY KEY CLUSTERED ([ListID] ASC),
    CONSTRAINT [FK_GameList_User] FOREIGN KEY ([UserID]) REFERENCES [dbo].[User] ([UserID]) ON DELETE CASCADE,
    CONSTRAINT [CK_GameList_Visibility] CHECK ([Visibility] BETWEEN 1 AND 3)
);
GO

CREATE NONCLUSTERED INDEX [IX_GameList_UserID]
    ON [dbo].[GameList] ([UserID] ASC, [UpdatedAt] DESC);
GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Lists of games an account makes to share ("Every Final Fantasy, ranked"). Unlike collections, a list can hold games that aren''t in the library. Ranked: the order is a ranking (numbers shown). Visibility: 1 everyone on this Ludex, 2 friends, 3 only the owner.',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'GameList';
