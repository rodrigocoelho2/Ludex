CREATE TABLE [dbo].[Notification]
(
    [NotificationID] BIGINT         IDENTITY (1, 1) NOT NULL,
    [UserID]         BIGINT         NOT NULL,
    [GameID]         INT            NULL,
    [Kind]           TINYINT        NOT NULL,
    [Text]           NVARCHAR (300) NOT NULL,
    [Url]            NVARCHAR (200) NULL,
    [CreatedAt]      DATETIME2 (3)  NOT NULL CONSTRAINT [DF_Notification_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [ReadAt]         DATETIME2 (3)  NULL,
    CONSTRAINT [PK_Notification] PRIMARY KEY CLUSTERED ([NotificationID] ASC),
    CONSTRAINT [FK_Notification_User] FOREIGN KEY ([UserID]) REFERENCES [dbo].[User] ([UserID]) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_Notification_User]
    ON [dbo].[Notification] ([UserID] ASC, [CreatedAt] DESC)
    INCLUDE ([ReadAt]);
GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'What the bell shows. Kind: 1 a wishlist price under its target, 2 new games from the Steam sync.',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'Notification';
