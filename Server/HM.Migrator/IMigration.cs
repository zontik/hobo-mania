namespace HM.Migrator;

public interface IMigration
{
    ValueTask Execute(Database db, CancellationToken ct);
}