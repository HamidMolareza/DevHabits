using DevHabits.Api.Database;
using DevHabits.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabits.Api.Controllers;

[Route("habits/{habitId}/tags/{tagId}")]
public class HabitTagsController(ApplicationDbContext context) : BaseApiController {
    [HttpPut]
    public async Task<IActionResult> AddTagToHabit(string habitId, string tagId, CancellationToken cancellationToken) {
        Habit? habit = await context.Habits
            .Include(habit => habit.HabitTags)
            .FirstOrDefaultAsync(habit => habit.Id == habitId, cancellationToken);
        if (habit is null)
            return NotFoundProblem(resource: "Habit", resourceId: habitId);

        // If the tag is already associated with the habit, do nothing
        if (habit.HabitTags.Any(habitTags => habitTags.TagId == tagId))
            return NoContent();

        // Check if the tag exists
        if (!await context.Tags.AnyAsync(tag => tag.Id == tagId, cancellationToken))
            return NotFoundProblem(resource: "Tag", resourceId: tagId);

        context.HabitTags.Add(new HabitTag { HabitId = habitId, TagId = tagId, CreatedDateTime = DateTime.UtcNow });
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> RemoveTagFromHabit(string habitId, string tagId,
        CancellationToken cancellationToken) {
        HabitTag? habitTag =
            await context.HabitTags.FirstOrDefaultAsync(habitTag =>
                habitTag.HabitId == habitId && habitTag.TagId == tagId, cancellationToken);
        if (habitTag is null)
            return NotFoundProblem($"Can not find any association between habit {habitId} and tag {tagId}");

        context.HabitTags.Remove(habitTag);
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
