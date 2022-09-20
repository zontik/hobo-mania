using System.Runtime.CompilerServices;
using Npgsql;

namespace HM.Migrator;

public class Database
{
    private readonly string _connectionString;

    public Database(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<T?> Scalar<T>(CancellationToken ct, string commandText, params object[] args)
    {
        await using var connection = await CreateDbConnection(ct);
        await using var cmd = new NpgsqlCommand(commandText, connection);
        for (var i = 0; i < args.Length; ++i)
        {
            cmd.Parameters.AddWithValue($"p{i}", args[i]);
        }

        var dbResult = await cmd.ExecuteScalarAsync(ct);
        return dbResult is DBNull ? default : (T)dbResult!;
    }

    public async Task<int> NonQuery(CancellationToken ct, string commandText, params object[] args)
    {
        await using var connection = await CreateDbConnection(ct);
        await using var cmd = new NpgsqlCommand(commandText, connection);
        for (var i = 0; i < args.Length; ++i)
        {
            cmd.Parameters.AddWithValue($"p{i}", args[i]);
        }

        return await cmd.ExecuteNonQueryAsync(ct);
    }

    public async IAsyncEnumerable<T> Query<T>([EnumeratorCancellation] CancellationToken ct, string commandText, params object[] args)
    {
        await foreach (var reader in ExecuteReader(ct, commandText, args))
        {
            yield return await reader.GetFieldValueAsync<T>(0, ct);
        }
    }

    public async IAsyncEnumerable<(C0, C1)> Query<C0, C1>([EnumeratorCancellation] CancellationToken ct, string commandText, params object[] args)
    {
        await foreach (var reader in ExecuteReader(ct, commandText, args))
        {
            yield return (await reader.GetFieldValueAsync<C0>(0, ct), await reader.GetFieldValueAsync<C1>(1, ct));
        }
    }

    public async IAsyncEnumerable<(C0, C1, C2)> Query<C0, C1, C2>([EnumeratorCancellation] CancellationToken ct, string commandText, params object[] args)
    {
        await foreach (var reader in ExecuteReader(ct, commandText, args))
        {
            yield return (await reader.GetFieldValueAsync<C0>(0, ct), await reader.GetFieldValueAsync<C1>(1, ct), await reader.GetFieldValueAsync<C2>(2, ct));
        }
    }

    private async IAsyncEnumerable<NpgsqlDataReader> ExecuteReader([EnumeratorCancellation] CancellationToken ct, string commandText, params object[] args)
    {
        await using var connection = await CreateDbConnection(ct);
        await using var cmd = new NpgsqlCommand(commandText, connection);
        for (var i = 0; i < args.Length; ++i)
        {
            cmd.Parameters.AddWithValue($"p{i}", args[i]);
        }

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            yield return reader;
        }
    }

    private async Task<NpgsqlConnection> CreateDbConnection(CancellationToken ct)
    {
        var connString = new NpgsqlConnectionStringBuilder(_connectionString)
        {
            CommandTimeout = 600
        };
        var conn = new NpgsqlConnection(connString.ConnectionString);
        await conn.OpenAsync(ct);
        return conn;
    }
}