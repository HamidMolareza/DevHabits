using System.Linq.Expressions;
using DevHabits.Api.Habits.Entities;
using DevHabits.Api.Tags.Dtos;

namespace DevHabits.Api.Habits.Dtos;

internal static class HabitQueries {
    public static Expression<Func<Habit, HabitResponse>> ProjectToDto() {
        return h => new HabitResponse {
            Id = h.Id,
            Name = h.Name,
            Description = h.Description,
            Type = h.Type,
            Frequency = new FrequencyDto { Type = h.Frequency.Type, TimesPerPeriod = h.Frequency.TimesPerPeriod },
            Target = new TargetDto { Value = h.Target.Value, Unit = h.Target.Unit },
            Status = h.Status,
            IsArchived = h.IsArchived,
            EndDate = h.EndDate,
            Milestone =
                h.Milestone == null
                    ? null
                    : new MilestoneDto { Target = h.Milestone.Target, Current = h.Milestone.Current },
            CreatedAtUtc = h.CreatedAtUtc,
            UpdatedAtUtc = h.UpdatedAtUtc,
            LastCompletedAtUtc = h.LastCompletedAtUtc
        };
    }

    public static Expression<Func<Habit, HabitWithTagsResponse>> ProjectToHabitWithTagsDto() {
        return h => new HabitWithTagsResponse {
            Id = h.Id,
            Name = h.Name,
            Description = h.Description,
            Type = h.Type,
            Frequency = new FrequencyDto { Type = h.Frequency.Type, TimesPerPeriod = h.Frequency.TimesPerPeriod },
            Target = new TargetDto { Value = h.Target.Value, Unit = h.Target.Unit },
            Status = h.Status,
            IsArchived = h.IsArchived,
            EndDate = h.EndDate,
            Milestone =
                h.Milestone == null
                    ? null
                    : new MilestoneDto { Target = h.Milestone.Target, Current = h.Milestone.Current },
            CreatedAtUtc = h.CreatedAtUtc,
            UpdatedAtUtc = h.UpdatedAtUtc,
            LastCompletedAtUtc = h.LastCompletedAtUtc,
            Tags = h.HabitTags.Select(ht => new TagResponse {
                Id = ht.Tag.Id,
                Name = ht.Tag.Name,
                Description = ht.Tag.Description,
                CreatedAtUtc = ht.Tag.CreatedAtUtc,
                UpdatedAtUtc = ht.Tag.UpdatedAtUtc,
            }).ToList()
        };
    }
}
