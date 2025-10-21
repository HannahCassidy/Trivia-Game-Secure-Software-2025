using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TriviaApi.Controllers; 

var builder = WebApplication.CreateBuilder(args);

// Connection string 
var connString = builder.Configuration.GetConnectionString("TriviaDb")
                 ?? "Server=localhost;Database=trivia;Trusted_Connection=True;TrustServerCertificate=True";

builder.Services.AddDbContext<TriviaDbContext>(opt =>
    opt.UseSqlServer(connString));

builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// CORS for unity
const string UnityCors = "UnityCors";
builder.Services.AddCors(o =>
{
    o.AddPolicy(UnityCors, p => p
        .AllowAnyHeader()
        .AllowAnyMethod()
        .SetIsOriginAllowed(_ => true)); 
});

var app = builder.Build();


app.UseRouting();
app.UseCors(UnityCors);

app.MapControllers();
app.MapGet("/", () => Results.Text("Trivia API is running", "text/plain"));

app.Run();

