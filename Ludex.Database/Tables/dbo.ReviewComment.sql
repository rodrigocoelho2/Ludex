CREATE TABLE [dbo].[ReviewComment]
(
    [CommentID] BIGINT          IDENTITY (1, 1) NOT NULL,
    [ReviewID]  BIGINT          NOT NULL,
    [UserID]    BIGINT          NOT NULL,
    [Text]      NVARCHAR (1000) NOT NULL,
    [At]        DATETIME2 (3)   NOT NULL CONSTRAINT [DF_ReviewComment_At] DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_ReviewComment] PRIMARY KEY CLUSTERED ([CommentID] ASC),
    CONSTRAINT [FK_ReviewComment_Review] FOREIGN KEY ([ReviewID]) REFERENCES [dbo].[Review] ([ReviewID]) ON DELETE CASCADE,
    CONSTRAINT [FK_ReviewComment_User] FOREIGN KEY ([UserID]) REFERENCES [dbo].[User] ([UserID])
);
GO

CREATE NONCLUSTERED INDEX [IX_ReviewComment_ReviewID]
    ON [dbo].[ReviewComment] ([ReviewID] ASC, [At] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_ReviewComment_UserID]
    ON [dbo].[ReviewComment] ([UserID] ASC);
GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Comments under a review, oldest first. They go with the review; an account''s own comments are removed by UserDelete.',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'ReviewComment';
