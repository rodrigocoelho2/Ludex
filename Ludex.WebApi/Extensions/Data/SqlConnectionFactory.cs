using Microsoft.Data.SqlClient;

namespace Ludex.WebApi.Extensions.Data;

/// <summary>
/// Opens connections to the Ludex SQL Server database.
/// </summary>
internal interface ISqlConnectionFactory
{
    /// <summary>
    /// Opens a new connection. The caller disposes it.
    /// </summary>
    /// <returns>An open connection.</returns>
    Task<SqlConnection> OpenAsync();
}

/// <summary>
/// Connections from the "Ludex" connection string (env <c>ConnectionStrings__Ludex</c>).
/// </summary>
internal sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    /// <summary>The name of the connection string.</summary>
    public const string ConnectionStringName = "Ludex";

    private readonly string _connectionString;

    /// <summary>
    /// Reads the connection string.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    public SqlConnectionFactory(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"ConnectionStrings:{ConnectionStringName} is empty (env ConnectionStrings__{ConnectionStringName}). Set it (docker-compose.yml builds it from SQL_APP_PASSWORD).");
        }

        _connectionString = new SqlConnectionStringBuilder(connectionString)
        {
            ApplicationName = "Ludex",
        }.ConnectionString;
    }

    /// <inheritdoc />
    public async Task<SqlConnection> OpenAsync()
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }
}
