/*
    DemoStatus
    How many made-up demo accounts there are, and how many reviews they wrote.
*/
CREATE PROCEDURE [dbo].[DemoStatus]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT (SELECT COUNT(*) FROM [dbo].[User] WHERE [IsDemo] = 1) AS [Accounts],
           (SELECT COUNT(*) FROM [dbo].[Review] r INNER JOIN [dbo].[User] u ON u.[UserID] = r.[UserID] WHERE u.[IsDemo] = 1) AS [Reviews],
           (SELECT COUNT(*) FROM [dbo].[User] WHERE [IsDemo] = 0) AS [RealAccounts];
END
