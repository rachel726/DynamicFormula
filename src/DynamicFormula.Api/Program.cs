// ╔══════════════════════════════════════════════════════════════════╗
// ║  DynamicFormula.Api — Program.cs                                 ║
// ║  ASP.NET Core Minimal API                                        ║
// ║  מחשף נתוני ביצועים מה-DB ל-Angular Dashboard                   ║
// ║                                                                  ║
// ║  Endpoints:                                                      ║
// ║    GET /api/performance  — זמני ריצה × שיטה × נוסחה             ║
// ║    GET /api/formulas     — רשימת נוסחאות מ-t_targil              ║
// ║    GET /swagger          — Swagger UI                            ║
// ╚══════════════════════════════════════════════════════════════════╝
using DynamicFormula.Data;

var builder = WebApplication.CreateBuilder(args);

// ── Connection String ──────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? @"Server=DESKTOP-H70Q0IN\SQLEXPRESS;Database=DynamicFormula;" +
       @"Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=30;";

// ── Services ───────────────────────────────────────────────────────
builder.Services.AddScoped<FormulaRepository>(_ => new FormulaRepository(connectionString));

// CORS — מאפשר ל-Angular (localhost:4200) לקרוא מהAPI
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new()
    {
        Title       = "Dynamic Formula Engine API",
        Version     = "v1",
        Description = "API למנוע חישוב נוסחאות דינמי — מבדק פיתוח רמה ג׳ משרד החינוך"
    }));

var app = builder.Build();

// ── Middleware ─────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dynamic Formula API v1");
    c.RoutePrefix = "swagger";
});
app.UseCors();

// ── Endpoints ──────────────────────────────────────────────────────

// GET /api/performance
// מחזיר זמני ריצה מ-t_log: שורה לכל נוסחה, עמודות לכל שיטה
app.MapGet("/api/performance", async (FormulaRepository repo) =>
{
    try
    {
        var data = await repo.GetPerformanceReportAsync();
        return Results.Ok(data);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail:     ex.Message,
            title:      "Failed to load performance data",
            statusCode: 500);
    }
})
.WithName("GetPerformance")
.WithSummary("השוואת זמני ביצוע")
.WithDescription("מחזיר מ-t_log את זמן הריצה לכל נוסחה בכל שיטה (SQL / CSHARP / NODEJS)");

// GET /api/formulas
// מחזיר את כל הנוסחאות מ-t_targil
app.MapGet("/api/formulas", async (FormulaRepository repo) =>
{
    try
    {
        var data = await repo.GetAllFormulasAsync();
        return Results.Ok(data);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail:     ex.Message,
            title:      "Failed to load formulas",
            statusCode: 500);
    }
})
.WithName("GetFormulas")
.WithSummary("רשימת נוסחאות")
.WithDescription("מחזיר את כל הנוסחאות מטבלת t_targil");

// בריצה ישירה מהדפדפן — redirect ל-Swagger
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();
