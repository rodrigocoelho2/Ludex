/*
    Post-deployment script of Ludex.Database.
    Runs after every publish of the dacpac. There is no reference data to seed: accounts are created from the Website
    (the first one becomes the admin) and games are copied from the catalog when someone adds or opens them.

    It creates the login the WebApi connects with ($(AppUser), password $(AppPassword) — SQL_APP_PASSWORD in .env), or
    brings its password in line with .env, and lets it read, write and run the procedures of this database only. On a
    shared SQL Server (shared-sql on the NAS) every app has its own login like this one, and only db-deploy uses sa.
*/
IF N'$(AppPassword)' <> N''
BEGIN
    DECLARE @AppUser SYSNAME = N'$(AppUser)';
    DECLARE @AppPassword NVARCHAR (128) = N'$(AppPassword)';
    DECLARE @Sql NVARCHAR (MAX);

    IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE [name] = @AppUser)
        SET @Sql = N'CREATE LOGIN ' + QUOTENAME(@AppUser) + N' WITH PASSWORD = ' + QUOTENAME(@AppPassword, '''')
                 + N', CHECK_POLICY = OFF, DEFAULT_DATABASE = ' + QUOTENAME(DB_NAME()) + N';';
    ELSE
        SET @Sql = N'ALTER LOGIN ' + QUOTENAME(@AppUser) + N' WITH PASSWORD = ' + QUOTENAME(@AppPassword, '''') + N', CHECK_POLICY = OFF;';
    EXEC (@Sql);

    IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE [name] = @AppUser)
    BEGIN
        SET @Sql = N'CREATE USER ' + QUOTENAME(@AppUser) + N' FOR LOGIN ' + QUOTENAME(@AppUser) + N';';
        EXEC (@Sql);
    END

    SET @Sql = N'ALTER ROLE [db_datareader] ADD MEMBER ' + QUOTENAME(@AppUser) + N'; '
             + N'ALTER ROLE [db_datawriter] ADD MEMBER ' + QUOTENAME(@AppUser) + N'; '
             + N'GRANT EXECUTE ON SCHEMA::[dbo] TO ' + QUOTENAME(@AppUser) + N';';
    EXEC (@Sql);

    PRINT N'Login ' + @AppUser + N' is ready.';
END

PRINT N'Ludex.Database deployed.';
