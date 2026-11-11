/*
    ProfileAvatarSave
    Replaces the profile photo (NULL @Avatar removes it) and returns the new AvatarVersion (NULL when removed).
*/
CREATE PROCEDURE [dbo].[ProfileAvatarSave]
    @UserUniqueID UNIQUEIDENTIFIER,
    @Avatar       VARBINARY (MAX),
    @ContentType  VARCHAR (40)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);
    IF @UserID IS NULL
    BEGIN
        SELECT CAST(NULL AS INT) AS [AvatarVersion];
        RETURN;
    END

    MERGE [dbo].[Profile] WITH (HOLDLOCK) AS t
    USING (SELECT @UserID AS [UserID]) AS s
    ON t.[UserID] = s.[UserID]
    WHEN MATCHED THEN
        UPDATE SET [Avatar] = @Avatar,
                   [AvatarContentType] = CASE WHEN @Avatar IS NULL THEN NULL ELSE @ContentType END,
                   [AvatarVersion] = CASE WHEN @Avatar IS NULL THEN NULL ELSE ISNULL(t.[AvatarVersion], 0) + 1 END,
                   [UpdatedAt] = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
        INSERT ([UserID], [Avatar], [AvatarContentType], [AvatarVersion])
        VALUES (@UserID, @Avatar, CASE WHEN @Avatar IS NULL THEN NULL ELSE @ContentType END, CASE WHEN @Avatar IS NULL THEN NULL ELSE 1 END);

    SELECT [AvatarVersion] FROM [dbo].[Profile] WHERE [UserID] = @UserID;
END
