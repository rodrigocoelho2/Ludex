CREATE TABLE [dbo].[User]
(
    [UserID]        BIGINT           IDENTITY (1, 1) NOT NULL,
    [UserUniqueID]  UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_User_UserUniqueID] DEFAULT (NEWID()),
    [Username]      NVARCHAR (32)    COLLATE Latin1_General_CI_AS NOT NULL,
    [PasswordHash]  NVARCHAR (200)   NOT NULL,
    [IsAdmin]       BIT              NOT NULL CONSTRAINT [DF_User_IsAdmin] DEFAULT (0),
    [SecurityStamp] NVARCHAR (64)    NOT NULL,
    [CreatedAt]     DATETIME2 (3)    NOT NULL CONSTRAINT [DF_User_CreatedAt] DEFAULT (SYSUTCDATETIME()),
    [LastLoginAt]   DATETIME2 (3)    NULL,
    [DiscordID]     VARCHAR (32)     NULL,
    [DiscordName]   NVARCHAR (64)    NULL,
    [IsDemo]        BIT              NOT NULL CONSTRAINT [DF_User_IsDemo] DEFAULT (0),
    CONSTRAINT [PK_User] PRIMARY KEY CLUSTERED ([UserID] ASC),
    CONSTRAINT [UQ_User_UserUniqueID] UNIQUE NONCLUSTERED ([UserUniqueID] ASC),
    CONSTRAINT [UQ_User_Username] UNIQUE NONCLUSTERED ([Username] ASC)
);
GO

-- One Discord account signs in to one Ludex account at most.
CREATE UNIQUE NONCLUSTERED INDEX [UX_User_DiscordID]
    ON [dbo].[User] ([DiscordID] ASC) WHERE [DiscordID] IS NOT NULL;
GO

EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Accounts. Usernames are unique ignoring case; the first account is the admin. DiscordID (with the Discord name it had) is set when the person links Discord in Settings, or signs up with Discord, and then signs in to this account. An account made with Discord has an empty PasswordHash (it never verifies) until a password is set. IsDemo marks the made-up accounts of the demo reviews (Seed:DemoReviews, or Settings): they have no password, can''t sign in, never count as the first account, and DemoRemove deletes them with everything they wrote. Every library entry, friendship and activity belongs to one account.',
    @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'User';
