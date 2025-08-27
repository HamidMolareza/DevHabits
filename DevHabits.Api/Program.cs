using System.Diagnostics;
using DevHabits.Api.Database;
using DevHabits.Api.Extensions;
using DevHabits.Api.Helpers.Sort;
using DevHabits.Api.Middlewares;
using FluentValidation;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSortOptionsFromAssemblyContaining<HabitSortOptionsProvider>();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddProblemDetails(options => {
    options.CustomizeProblemDetails = context => {
        context.ProblemDetails.Instance =
            $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";

        context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);

        Activity? activity = context.HttpContext.Features.Get<IHttpActivityFeature>()?.Activity;
        context.ProblemDetails.Extensions.TryAdd("traceId", activity?.Id);
    };
});
builder.Services
    .AddExceptionHandler<ValidationExceptionHandler>()
    .AddExceptionHandler<SortExceptionHandler>();

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

app.UseExceptionHandler();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
