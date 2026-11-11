/*
    FriendList
    The friends of an account and the requests waiting either way, with how many games each has.
    State: 1 friends, 2 request sent, 3 request received.
*/
CREATE PROCEDURE [dbo].[FriendList]
    @UserUniqueID UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

    DECLARE @UserID BIGINT = (SELECT [UserID] FROM [dbo].[User] WHERE [UserUniqueID] = @UserUniqueID);

    WITH [Links] AS
    (
        SELECT f.[FriendUserID] AS [OtherID],
               CAST(CASE WHEN f.[AcceptedAt] IS NULL THEN 2 ELSE 1 END AS TINYINT) AS [State],
               COALESCE(f.[AcceptedAt], f.[CreatedAt]) AS [Since]
        FROM [dbo].[Friendship] f
        WHERE f.[UserID] = @UserID

        UNION ALL

        SELECT f.[UserID], CAST(3 AS TINYINT), f.[CreatedAt]
        FROM [dbo].[Friendship] f
        WHERE f.[FriendUserID] = @UserID
          AND f.[AcceptedAt] IS NULL
          AND NOT EXISTS (SELECT 1 FROM [dbo].[Friendship] r WHERE r.[UserID] = @UserID AND r.[FriendUserID] = f.[UserID])
    )
    SELECT u.[UserUniqueID],
           u.[Username],
           l.[State],
           l.[Since],
           (SELECT COUNT(*) FROM [dbo].[LibraryEntry] e WHERE e.[UserID] = u.[UserID]) AS [GameCount]
    FROM [Links] l
    INNER JOIN [dbo].[User] u ON u.[UserID] = l.[OtherID]
    ORDER BY l.[State], u.[Username];
END
