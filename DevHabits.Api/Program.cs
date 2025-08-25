using DevHabits.Api.Database;
using DevHabits.Api.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("default"),
        mssqlOptions => mssqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Application)
    ).UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
);

builder.Services.AddOpenTelemetryExtensions(builder.Environment.ApplicationName);

builder.Logging.AddOpenTelemetry(options => {
    options.IncludeScopes = true;
    options.IncludeFormattedMessage = true;
});

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();

    await app.Services.ApplyMigrationsAsync();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
