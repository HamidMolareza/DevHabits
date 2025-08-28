using System.Reflection;

namespace DevHabits.Api.Shared.Libraries.DataShaping;

public sealed class DataShapeMapping {
    private readonly Dictionary<(Type Entity, Type Dto), object> _configs = [];

    public DataShapeMapping(Assembly assembly) {
        var configuratorTypes = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType &&
                            i.GetGenericTypeDefinition() == typeof(IDataShapingConfigurator<,>))
                .Select(i => new { Implementation = t, Service = i }));

        foreach (var ct in configuratorTypes) {
            // Create an instance of the configurator
            object configurator = Activator.CreateInstance(ct.Implementation)!;

            Type entityType = ct.Service.GetGenericArguments()[0];
            Type dtoType = ct.Service.GetGenericArguments()[1];

            Type configType = typeof(DtoMappingConfiguration<,>).MakeGenericType(entityType, dtoType);
            object config = Activator.CreateInstance(configType)!;

            // Call Configure(config)
            MethodInfo method = ct.Service.GetMethod("Configure")!;
            method.Invoke(configurator, [config]);

            _configs[(entityType, dtoType)] = config;
        }
    }

    public DtoMappingConfiguration<TEntity, TDto> Get<TEntity, TDto>()
        where TEntity : class
        where TDto : class {
        if (!_configs.TryGetValue((typeof(TEntity), typeof(TDto)), out object? config))
            throw new InvalidOperationException(
                $"No IDataShapingConfigurator<{typeof(TEntity).Name}, {typeof(TDto).Name}> found.");

        return (DtoMappingConfiguration<TEntity, TDto>)config;
    }
}
