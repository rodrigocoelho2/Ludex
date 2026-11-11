/*
    PriceSave
    Keeps a game's best current price, then alerts every account that wishlisted it with a target the price has
    reached (once per new low under the target; going back over the target re-arms the alert).
*/
CREATE PROCEDURE [dbo].[PriceSave]
    @GameID   INT,
    @ItadID   VARCHAR (40),
    @Amount   DECIMAL (9, 2),
    @Regular  DECIMAL (9, 2),
    @Cut      TINYINT,
    @Currency CHAR (3),
    @Shop     NVARCHAR (60),
    @Url      NVARCHAR (500),
    @Low      DECIMAL (9, 2)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    MERGE [dbo].[GamePrice] WITH (HOLDLOCK) AS t
    USING (SELECT @GameID AS [GameID]) AS s
    ON t.[GameID] = s.[GameID]
    WHEN MATCHED THEN
        UPDATE SET [ItadID] = @ItadID, [Amount] = @Amount, [Regular] = @Regular, [Cut] = @Cut, [Currency] = @Currency,
                   [Shop] = @Shop, [Url] = @Url, [Low] = @Low, [CheckedAt] = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT ([GameID], [ItadID], [Amount], [Regular], [Cut], [Currency], [Shop], [Url], [Low])
        VALUES (@GameID, @ItadID, @Amount, @Regular, @Cut, @Currency, @Shop, @Url, @Low);

    IF @Amount IS NOT NULL
    BEGIN
        DECLARE @Name NVARCHAR (300) = (SELECT [Name] FROM [dbo].[Game] WHERE [GameID] = @GameID);

        INSERT INTO [dbo].[Notification] ([UserID], [GameID], [Kind], [Text], [Url])
        SELECT e.[UserID], @GameID, 1,
               LEFT(CONCAT(@Name, N' is ', FORMAT(@Amount, N'0.00'), N' ', @Currency, N' at ', @Shop,
                           CASE WHEN @Cut > 0 THEN CONCAT(N' (−', @Cut, N'%)') ELSE N'' END,
                           N': under your ', FORMAT(e.[PriceTarget], N'0.00'), N' target.'), 300),
               CONCAT(N'/games/', @GameID)
        FROM [dbo].[LibraryEntry] e
        WHERE e.[GameID] = @GameID AND e.[Status] = 3 AND e.[PriceTarget] IS NOT NULL
          AND @Amount <= e.[PriceTarget]
          AND (e.[PriceAlerted] IS NULL OR @Amount < e.[PriceAlerted]);

        UPDATE [dbo].[LibraryEntry]
        SET [PriceAlerted] = CASE WHEN @Amount <= [PriceTarget] THEN @Amount ELSE NULL END
        WHERE [GameID] = @GameID AND [Status] = 3 AND [PriceTarget] IS NOT NULL
          AND (@Amount > [PriceTarget] OR [PriceAlerted] IS NULL OR @Amount < [PriceAlerted]);
    END

    COMMIT TRANSACTION;
END
