using HM.Migrator;
using Microsoft.Extensions.Configuration;

Console.WriteLine("Database Migrator.");

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.Local.json", true)
    .AddCommandLine(args)
    .Build();

var connectionString = configuration.GetSection("Database:ConnectionString").Value;
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Connection string was not specified");
    return 1;
}

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => {
    cts.Cancel();
    e.Cancel = true;
};

try
{
    await Migrator.Migrate(connectionString, cts.Token);
}
catch (OperationCanceledException)
{
    return 1;
}
catch (Exception e)
{
    Console.Error.WriteLine($"{e.Message}\n{e.StackTrace}");
    return 1;
}

return 0;