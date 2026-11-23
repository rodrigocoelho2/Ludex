/*
    ListSave
    Creates a list (@ListID NULL) or changes one of the account's own, and returns its id (0: not the account's).
    Visibility: 1 everyone, 2 friends, 3 only me.
*/
CREATE PROCEDURE [dbo].[ListSave]
    @UserUniqueID UNIQUEIDENTIFIER,
    @ListID       INT,
    @Name         NVARCHAR (80),
    @Description  NVARCHAR (1000),
    @Ranked       BIT,
    @Visibility   TINYINT,
    @At           DATETIME2 (3) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);
    DECLARE @Result INT = 0;
    DECLARE @When DATETIME2 (3) = COALESCE(@At, SYSUTCDATETIME());

    IF @UserID IS NOT NULL
    BEGIN
        IF @ListID IS NULL
        BEGIN
            INSERT INTO [dbo].[GameList] ([UserID], [Name], [Description], [Ranked], [Visibility], [CreatedAt], [UpdatedAt])
            VALUES (@UserID, @Name, @Description, @Ranked, @Visibility, @When, @When);
            SET @Result = CAST(SCOPE_IDENTITY() AS INT);
        END
        ELSE
        BEGIN
            UPDATE [dbo].[GameList]
            SET [Name] = @Name, [Description] = @Description, [Ranked] = @Ranked, [Visibility] = @Visibility, [UpdatedAt] = @When
            WHERE [ListID] = @ListID AND [UserID] = @UserID;
            IF @@ROWCOUNT = 1
                SET @Result = @ListID;
        END
    END

    SELECT @Result AS [ListID];
END
