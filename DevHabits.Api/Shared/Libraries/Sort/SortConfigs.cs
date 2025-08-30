using System.Reflection;

namespace DevHabits.Api.Shared.Libraries.Sort;

public sealed class SortConfigs {
    private readonly Dictionary<Type, SortOptions> _configs = [];

    public SortConfigs(Assembly assembly) {
        var configuratorTypes = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType &&
                            i.GetGenericTypeDefinition() == typeof(ISortOptionsProvider<>))
                .Select(i => new { Implementation = t, Service = i }));

        foreach (var ct in configuratorTypes) {
            // Create an instance of the configurator
            object configurator = Activator.CreateInstance(ct.Implementation)!;

            Type entityType = ct.Service.GetGenericArguments()[0];

            // Call Configure(config)
            MethodInfo method = ct.Service.GetMethod("GetOptions")!;
            object? result = method.Invoke(configurator, null);

            if (result is not SortOptions sortOption) {
                throw new Exception(
                    $"Expected to get SortOptions from GetOptions() but got {result?.GetType().Name ?? "null"}");
            }

            _configs[entityType] = sortOption;
        }
    }

    public SortOptions Get<TEntity>()
        where TEntity : class {
        if (!_configs.TryGetValue(typeof(TEntity), out SortOptions? config))
            throw new InvalidOperationException(
                $"No ISortOptionsProvider<{typeof(TEntity).Name}> found.");

        return config;
    }
}
