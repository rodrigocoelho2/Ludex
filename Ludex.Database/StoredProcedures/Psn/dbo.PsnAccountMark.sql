/*
    PsnAccountMark
    Records that an account's PlayStation library was synced (@Synced = 1) and/or that the bell warned about its
    connection (@Warned = 1).
*/
CREATE PROCEDURE [dbo].[PsnAccountMark]
    @UserUniqueID UNIQUEIDENTIFIER,
    @Synced       BIT,
    @Warned       BIT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE a
    SET [SyncedAt] = CASE WHEN @Synced = 1 THEN SYSUTCDATETIME() ELSE a.[SyncedAt] END,
        [WarnedAt] = CASE WHEN @Warned = 1 THEN SYSUTCDATETIME() ELSE a.[WarnedAt] END
    FROM [dbo].[PsnAccount] a
    INNER JOIN [dbo].[User] u ON u.[UserID] = a.[UserID]
    WHERE u.[UserUniqueID] = @UserUniqueID;
END
