CREATE TABLE [dbo].[GamePrice]
(
    [GameID]    INT            NOT NULL,
    [ItadID]    VARCHAR (40)   NULL,
    [Amount]    DECIMAL (9, 2) NULL,
    [Regular]   DECIMAL (9, 2) NULL,
    [Cut]       TINYINT        NULL,
    [Currency]  CHAR (3)       NULL,
    [Shop]      NVARCHAR (60)  NULL,
    [Url]       NVARCHAR (500) NULL,
    [Low]       DECIMAL (9, 2) NULL,
    [CheckedAt] DATETIME2 (3)  NOT NULL CONSTRAINT [DF_GamePrice_CheckedAt] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_GamePrice] PRIMARY KEY CLUSTERED ([GameID] ASC),
    CONSTRAINT [FK_GamePrice_Game] FOREIGN KEY ([GameID]) REFERENCES [dbo].[Game] ([GameID]) ON DELETE CASCADE
);
GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'The best current PC price of a wishlisted game (IsThereAnyDeal), in the configured country: Amount at Shop (Url), Regular price, Cut (% off), Low = the lowest price ever. ItadID is IsThereAnyDeal''s id (null Amount: not sold right now).',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'GamePrice';
