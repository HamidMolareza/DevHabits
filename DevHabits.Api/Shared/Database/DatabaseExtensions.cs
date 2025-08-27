using Microsoft.EntityFrameworkCore;

namespace DevHabits.Api.Shared.Database;

public static class DatabaseExtensions {
    public static async Task ApplyMigrationsAsync(this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default) {
        using IServiceScope scope = serviceProvider.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        ILogger logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        try {
            await dbContext.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Database migrations applied successfully.");
        }
        catch (Exception e) {
            logger.LogError(e, "An error occurred while applying database migrations.");
            throw;
        }
    }
}
