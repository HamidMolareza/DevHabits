using System.Collections.ObjectModel;
using DevHabits.Api.Habits.Entities;
using DevHabits.Api.Shared.Libraries.DataShaping;
using DevHabits.Api.Shared.Libraries.Hateoas;

namespace DevHabits.Api.Habits.Dtos;

public sealed record HabitResponse {
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required HabitType Type { get; init; }
    public required FrequencyDto Frequency { get; init; }
    public required TargetDto Target { get; init; }
    public required HabitStatus Status { get; init; }
    public required bool IsArchived { get; init; }
    public DateOnly? EndDate { get; init; }
    public MilestoneDto? Milestone { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public DateTime? LastCompletedAtUtc { get; init; }
    public ReadOnlyDictionary<string, LinkDto>? Links { get; set; }
}

public sealed record FrequencyDto {
    public required FrequencyType Type { get; init; }
    public required int TimesPerPeriod { get; init; }
}

public sealed record TargetDto {
    public required int Value { get; init; }
    public required string Unit { get; init; }
}

public sealed record MilestoneDto {
    public required int Target { get; init; }
    public required int Current { get; init; }
}

public sealed class HabitResponseDataShapingConfigurator : IDataShapingConfigurator<Habit, HabitResponse> {
    public void Configure(DtoMappingConfiguration<Habit, HabitResponse> configuration) {
        configuration.Map(
            dto => dto.Id,
            entity => entity.Id
        ); //TODO: Create .Required() method to enforce a prop always should be include;
        configuration.Map(
            dto => dto.Name,
            entity => entity.Name
        );
        configuration.Map(
            dto => dto.Description,
            entity => entity.Description
        );
        configuration.Map(
            dto => dto.Type,
            entity => entity.Type
        );
        configuration.MapComplex(
            dto => dto.Frequency,
            entity => entity.Frequency,
            milestone => {
                milestone.Map(d => d.Type, e => e.Type);
                milestone.Map(d => d.TimesPerPeriod, e => e.TimesPerPeriod);
            });
        configuration.MapComplex(
            dto => dto.Target,
            entity => entity.Target,
            milestone => {
                milestone.Map(d => d.Value, e => e.Value);
                milestone.Map(d => d.Unit, e => e.Unit);
            });
        configuration.Map(
            dto => dto.Status,
            entity => entity.Status
        );
        configuration.Map(
            dto => dto.IsArchived,
            entity => entity.IsArchived
        );
        configuration.Map(
            dto => dto.EndDate,
            entity => entity.EndDate
        );
        configuration.MapComplex(
            dto => dto.Milestone!,
            entity => entity.Milestone!,
            milestone => {
                milestone.Map(d => d.Target, e => e.Target);
                milestone.Map(d => d.Current, e => e.Current);
            });
        configuration.Map(
            dto => dto.CreatedAtUtc,
            entity => entity.CreatedAtUtc
        );
        configuration.Map(
            dto => dto.UpdatedAtUtc,
            entity => entity.UpdatedAtUtc
        );
        configuration.Map(
            dto => dto.LastCompletedAtUtc,
            entity => entity.LastCompletedAtUtc
        );
    }
}
