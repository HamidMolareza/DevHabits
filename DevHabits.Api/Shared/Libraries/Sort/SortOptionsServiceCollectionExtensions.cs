using System.Reflection;

namespace DevHabits.Api.Shared.Libraries.Sort;

public static class SortOptionsServiceCollectionExtensions {
    public static IServiceCollection AddSortOptionsFromAssemblyContaining<T>(this IServiceCollection services) {
        return AddSortOptionsFromAssembly(services, typeof(T).Assembly);
    }

    public static IServiceCollection AddSortOptionsFromAssembly(this IServiceCollection services, Assembly assembly) {
        var providerTypes = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISortOptionsProvider<>))
                .Select(i => new { Implementation = t, Service = i }));

        foreach (var p in providerTypes) {
            services.AddSingleton(p.Service, p.Implementation);
        }

        return services;
    }
}
