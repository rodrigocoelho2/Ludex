/*
    CommunityCounts
    How many people (not counting made-up demo accounts, Real) and accounts in all (All) there are, for sizing the
    community shelves.
*/
CREATE PROCEDURE [dbo].[CommunityCounts]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT SUM(CASE WHEN [IsDemo] = 0 THEN 1 ELSE 0 END) AS [Real], COUNT(*) AS [All]
    FROM [dbo].[User];
END
