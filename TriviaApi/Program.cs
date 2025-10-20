using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
// Explicit ASP.NET Core usings so IntelliSense is happy even if implicit usings are off
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// TODO: put your rotated password in place of <PWD>
var connStr = "Server=BALEGDEH;Database=trivia;User ID=hc2124;Password=aegis2025;Encrypt=True;TrustServerCertificate=True;";

// POST /session  -> returns { sessionId }
app.MapPost("/session", async (HttpContext ctx) =>
{
    using var db = new SqlConnection(connStr);
    var userId = "anon-" + Guid.NewGuid().ToString("N");
    var id = await db.ExecuteScalarAsync<int>(
        @"INSERT INTO dbo.Sessions(UserId) VALUES (@u);
          SELECT CAST(SCOPE_IDENTITY() AS INT);", new { u = userId });

    return Results.Ok(new { sessionId = id });
});

// GET /question -> returns { questionId, text, choices[] }
app.MapGet("/question", async () =>
{
    using var db = new SqlConnection(connStr);
    var row = await db.QueryFirstOrDefaultAsync<(int Id, string Text, string ChoicesJSON)>(
        "SELECT TOP 1 Id, Text, ChoicesJSON FROM dbo.Questions WHERE Active=1 ORDER BY NEWID();");

    if (row.Id == 0) return Results.NotFound();

    var choices = JsonSerializer.Deserialize<string[]>(row.ChoicesJSON) ?? Array.Empty<string>();
    return Results.Ok(new { questionId = row.Id, text = row.Text, choices });
});

// POST /answer?sessionId=&questionId=&choiceIndex= -> returns { correct, newScore }
app.MapPost("/answer", async (int sessionId, int questionId, int choiceIndex) =>
{
    using var db = new SqlConnection(connStr);
    var correctIdx = await db.QuerySingleOrDefaultAsync<int?>(
        "SELECT CorrectChoiceIndex FROM dbo.Answers WHERE QuestionId=@q", new { q = questionId });

    if (correctIdx is null) return Results.BadRequest(new { error = "unknown_question" });

    if (correctIdx == choiceIndex)
        await db.ExecuteAsync("UPDATE dbo.Sessions SET Score = Score + 1 WHERE Id=@s", new { s = sessionId });

    var score = await db.QuerySingleAsync<int>("SELECT Score FROM dbo.Sessions WHERE Id=@s", new { s = sessionId });

    return Results.Ok(new { correct = (correctIdx == choiceIndex), newScore = score });
});

app.Run();
