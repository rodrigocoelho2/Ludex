/*
    DemoUserCreate
    Creates one of the made-up accounts of the demo reviews (IsDemo 1, never an admin, no password: it can't sign in)
    with its profile, or finds it when it is there already, and returns its id. Returns no row when a real account
    has the name.
*/
CREATE PROCEDURE [dbo].[DemoUserCreate]
    @Username      NVARCHAR (32),
    @SecurityStamp NVARCHAR (64),
    @Bio           NVARCHAR (300),
    @Location      NVARCHAR (80),
    @Accent        VARCHAR (20),
    @CreatedAt     DATETIME2 (3)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @UserID BIGINT, @IsDemo BIT;
    SELECT @UserID = [UserID], @IsDemo = [IsDemo] FROM [dbo].[User] WITH (UPDLOCK, HOLDLOCK) WHERE [Username] = @Username;

    IF @UserID IS NULL
    BEGIN
        INSERT INTO [dbo].[User] ([Username], [PasswordHash], [IsAdmin], [SecurityStamp], [CreatedAt], [IsDemo])
        VALUES (@Username, N'', 0, @SecurityStamp, @CreatedAt, 1);
        SET @UserID = CAST(SCOPE_IDENTITY() AS BIGINT);
        SET @IsDemo = 1;

        INSERT INTO [dbo].[Profile] ([UserID], [Bio], [Location], [Accent], [LibraryVisibility])
        VALUES (@UserID, @Bio, @Location, @Accent, 2);
    END

    COMMIT TRANSACTION;

    SELECT [UserUniqueID]
    FROM [dbo].[User]
    WHERE [UserID] = @UserID AND @IsDemo = 1;
END
