using DevHabits.Api.Habits.Dtos;
using DevHabits.Api.HabitTags.Entities;

namespace DevHabits.Api.Habits.Entities;

public sealed class Habit {
    public required string Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public HabitType Type { get; set; }
    public Frequency Frequency { get; set; } = new();
    public Target Target { get; set; } = null!;
    public HabitStatus Status { get; set; }
    public bool IsArchived { get; set; }
    public DateOnly? EndDate { get; set; }
    public Milestone? Milestone { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? LastCompletedAtUtc { get; set; }

    public ICollection<HabitTag> HabitTags { get; set; } = null!;

    public HabitResponse ToHabitResponse() {
        return new HabitResponse {
            Id = Id,
            Name = Name,
            Description = Description,
            Type = Type,
            Frequency = new FrequencyDto {
                Type = Frequency.Type,
                TimesPerPeriod = Frequency.TimesPerPeriod
            },
            Target = new TargetDto {
                Value = Target.Value,
                Unit = Target.Unit
            },
            Status = Status,
            IsArchived = IsArchived,
            EndDate = EndDate,
            Milestone = Milestone == null
                ? null
                : new MilestoneDto {
                    Target = Milestone.Target,
                    Current = Milestone.Current
                },
            CreatedAtUtc = CreatedAtUtc,
            UpdatedAtUtc = UpdatedAtUtc,
            LastCompletedAtUtc = LastCompletedAtUtc
        };
    }
}

public enum HabitType {
    None = 0,
    Binary = 1,
    Measurable = 2
}

public enum HabitStatus {
    None = 0,
    Ongoing = 1,
    Completed = 2
}

public sealed class Frequency {
    public FrequencyType Type { get; set; }
    public int TimesPerPeriod { get; set; }
}

public enum FrequencyType {
    None = 0,
    Daily = 1,
    Weekly = 2,
    Monthly = 3
}

public sealed class Target {
    public int Value { get; set; }
    public required string Unit { get; set; }
}

public sealed class Milestone {
    public int Target { get; set; }
    public int Current { get; set; }
}
