#pragma warning disable IDE0055

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace DevHabits.Api.Extensions;

public static class OpenTelemetryExtensions {
    public static void AddOpenTelemetryExtensions(this IServiceCollection services, string applicationName) {
        // Configure OpenTelemetry for distributed tracing and metrics
        services.AddOpenTelemetry()
            // Set resource information (service name)
            .ConfigureResource(resource => resource.AddService(applicationName))
            // Enable tracing with HTTP client and ASP.NET Core instrumentation
            .WithTracing(tracing => tracing
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation())
            // Enable metrics with HTTP client, ASP.NET Core, and runtime instrumentation
            .WithMetrics(metrics => metrics
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation())
            // Export telemetry data using OTLP protocol
            .UseOtlpExporter();
    }
}
