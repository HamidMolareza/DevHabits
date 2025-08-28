namespace DevHabits.Api.Shared.Libraries.DataShaping;

public interface IDataShapingConfigurator<TEntity, TDto> where TDto : class {
    void Configure(DtoMappingConfiguration<TEntity, TDto> configuration);
}
