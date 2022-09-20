using HM.Migrator.Migrations;

namespace HM.Migrator;

public class Migrator
{
    private static readonly (string Name, IMigration Executor)[] Migrations =
    {
        ("Init", new Migration_0())
    };

    public static async Task Migrate(string connectionString, CancellationToken ct)
    {
        var database = new Database(connectionString);
        await database.NonQuery(ct, "CREATE TABLE IF NOT EXISTS migrations (id SERIAL PRIMARY KEY, name TEXT, date TIMESTAMP DEFAULT now(), done BOOLEAN DEFAULT false)");

        var doneMigrations = await database.Query<string, bool>(ct, "SELECT name, done FROM migrations ORDER BY id").ToArrayAsync();
        if (doneMigrations.Any(x => !x.Item2))
        {
            throw new Exception("Not all migrations executed successfully");
        }

        if (!doneMigrations.Select(x => x.Item1).SequenceEqual(Migrations.Select(x => x.Name).Take(doneMigrations.Length)))
        {
            throw new Exception("Invalid migrations list");
        }

        foreach(var migration in Migrations.Skip(doneMigrations.Length))
        {
            var migrationId = await database.Scalar<int>(ct, "INSERT INTO migrations (name) VALUES (@p0) RETURNING id", migration.Name);
            await migration.Executor.Execute(database, ct);
            await database.NonQuery(ct, "UPDATE migrations SET done=TRUE WHERE id=@p0", migrationId);
        }
    }
}