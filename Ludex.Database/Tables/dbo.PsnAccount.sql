CREATE TABLE [dbo].[PsnAccount]
(
    [UserID]         BIGINT          NOT NULL,
    [OnlineID]       NVARCHAR (40)   NULL,
    [Token]          NVARCHAR (4000) NULL,
    [TokenExpiresAt] DATETIME2 (3)   NULL,
    [ConnectedAt]    DATETIME2 (3)   NOT NULL CONSTRAINT [DF_PsnAccount_ConnectedAt] DEFAULT (SYSUTCDATETIME()),
    [SyncedAt]       DATETIME2 (3)   NULL,
    [WarnedAt]       DATETIME2 (3)   NULL,
    CONSTRAINT [PK_PsnAccount] PRIMARY KEY CLUSTERED ([UserID] ASC),
    CONSTRAINT [FK_PsnAccount_User] FOREIGN KEY ([UserID]) REFERENCES [dbo].[User] ([UserID]) ON DELETE CASCADE
);
GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'An account''s PlayStation Network connection. Token is the PSN refresh token, encrypted by the WebApi (AES-GCM, with a key derived from LUDEX_API_KEY): it never leaves the WebApi in clear. TokenExpiresAt is when Sony stops accepting it (about two months); NULL Token = the connection broke and must be made again. WarnedAt: when the bell last said so. SyncedAt: the last daily sync.',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'PsnAccount';
