using System.Diagnostics;
using DevHabits.Api.Habits.Dtos;
using DevHabits.Api.Habits.Options;
using DevHabits.Api.Shared.Database;
using DevHabits.Api.Shared.Libraries.DataShaping;
using DevHabits.Api.Shared.Libraries.FluentValidationHelpers;
using DevHabits.Api.Shared.Libraries.Sort;
using DevHabits.Api.Shared.Middlewares;
using DevHabits.Api.Shared.ServiceCollections;
using FluentValidation;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(options => {
    options.Filters.Add<FluentValidationFilter>();
});

builder.Services.AddOpenApi();

builder.Services.AddSingleton(new SortConfigs(typeof(HabitSortOptionsProvider).Assembly));

builder.Services.AddSingleton(new DataShapeMapping(typeof(HabitResponseDataShapingConfigurator).Assembly));

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
    .AddExceptionHandler<SortExceptionHandler>()
    .AddExceptionHandler<ShapeDataExceptionHandler>();

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
