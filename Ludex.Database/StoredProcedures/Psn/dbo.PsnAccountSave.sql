/*
    PsnAccountSave
    Saves an account's PlayStation connection: the online id and the (encrypted) refresh token with when it runs out.
    A NULL @Token marks the connection as broken (kept, so the page can say so). A new token clears the warning.
*/
CREATE PROCEDURE [dbo].[PsnAccountSave]
    @UserUniqueID   UNIQUEIDENTIFIER,
    @OnlineID       NVARCHAR (40),
    @Token          NVARCHAR (4000),
    @TokenExpiresAt DATETIME2 (3)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);
    IF @UserID IS NULL
        RETURN;

    MERGE [dbo].[PsnAccount] WITH (HOLDLOCK) AS t
    USING (SELECT @UserID AS [UserID]) AS s
    ON t.[UserID] = s.[UserID]
    WHEN MATCHED THEN
        UPDATE SET [OnlineID] = ISNULL(@OnlineID, t.[OnlineID]),
                   [Token] = @Token,
                   [TokenExpiresAt] = @TokenExpiresAt,
                   [WarnedAt] = CASE WHEN @Token IS NOT NULL AND (t.[Token] IS NULL OR @TokenExpiresAt > ISNULL(t.[TokenExpiresAt], '19000101')) THEN NULL ELSE t.[WarnedAt] END
    WHEN NOT MATCHED THEN
        INSERT ([UserID], [OnlineID], [Token], [TokenExpiresAt]) VALUES (s.[UserID], @OnlineID, @Token, @TokenExpiresAt);
END
