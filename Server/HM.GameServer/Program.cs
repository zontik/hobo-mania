var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddJsonFile("appsettings.Local.json", true)
    .AddCommandLine(args);

var app = builder.Build();

app.MapGet("/", () => "Hello Hobo Mania!");

app.Run();