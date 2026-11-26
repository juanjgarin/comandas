var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Placeholder until the real endpoints arrive in step 1.15.
app.MapGet("/", () => "Orders.API is running");

app.Run();
