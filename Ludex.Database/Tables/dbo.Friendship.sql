CREATE TABLE [dbo].[Friendship]
(
    [UserID]       BIGINT        NOT NULL,
    [FriendUserID] BIGINT        NOT NULL,
    [CreatedAt]    DATETIME2 (3) NOT NULL CONSTRAINT [DF_Friendship_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [AcceptedAt]   DATETIME2 (3) NULL,
    CONSTRAINT [PK_Friendship] PRIMARY KEY CLUSTERED ([UserID] ASC, [FriendUserID] ASC),
    CONSTRAINT [FK_Friendship_User] FOREIGN KEY ([UserID]) REFERENCES [dbo].[User] ([UserID]),
    CONSTRAINT [FK_Friendship_Friend] FOREIGN KEY ([FriendUserID]) REFERENCES [dbo].[User] ([UserID]),
    CONSTRAINT [CK_Friendship_NotSelf] CHECK ([UserID] <> [FriendUserID])
);
GO

CREATE NONCLUSTERED INDEX [IX_Friendship_FriendUserID]
    ON [dbo].[Friendship] ([FriendUserID] ASC)
    INCLUDE ([AcceptedAt]);
GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Friend requests and friendships. A request is one row (sender → receiver, AcceptedAt NULL); accepting it adds the reverse row, so a friendship is two accepted rows and "my friends" is a single seek. Rows are removed by UserDelete (two foreign keys to User can''t both cascade).',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'Friendship';
