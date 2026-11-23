CREATE TABLE [dbo].[ReviewLike]
(
    [ReviewID] BIGINT        NOT NULL,
    [UserID]   BIGINT        NOT NULL,
    [At]       DATETIME2 (3) NOT NULL CONSTRAINT [DF_ReviewLike_At] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_ReviewLike] PRIMARY KEY CLUSTERED ([ReviewID] ASC, [UserID] ASC),
    CONSTRAINT [FK_ReviewLike_Review] FOREIGN KEY ([ReviewID]) REFERENCES [dbo].[Review] ([ReviewID]) ON DELETE CASCADE,
    CONSTRAINT [FK_ReviewLike_User] FOREIGN KEY ([UserID]) REFERENCES [dbo].[User] ([UserID])
);
GO

CREATE NONCLUSTERED INDEX [IX_ReviewLike_UserID]
    ON [dbo].[ReviewLike] ([UserID] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_ReviewLike_At]
    ON [dbo].[ReviewLike] ([At] DESC)
    INCLUDE ([ReviewID]);
GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Who liked which review (one like per account). The likes go with the review; an account''s own likes are removed by UserDelete (the review and the account can''t both cascade).',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'ReviewLike';
