/*
    LibraryLogSave
    The log details of one entry (Log a game): how it ended, and the dates it was started and finished.
*/
CREATE PROCEDURE [dbo].[LibraryLogSave]
    @UserUniqueID UNIQUEIDENTIFIER,
    @EntryID      BIGINT,
    @Ending       TINYINT,
    @StartedOn    DATE,
    @FinishedOn   DATE
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);

    UPDATE [dbo].[LibraryEntry]
    SET [Ending] = @Ending,
        [StartedOn] = @StartedOn,
        [FinishedOn] = @FinishedOn,
        [UpdatedAt] = SYSUTCDATETIME()
    WHERE [EntryID] = @EntryID AND [UserID] = @UserID;
END
