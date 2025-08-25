using DevHabits.Api.Database;
using DevHabits.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabits.Api.Controllers;

[Route("habits/{habitId}/tags/{tagId}")]
[ApiController]
public class HabitTagsController(ApplicationDbContext context) : ControllerBase {
    [HttpPut]
    public async Task<IActionResult> AddTagToHabit(string habitId, string tagId) {
        Habit? habit = await context.Habits
            .Include(habit => habit.HabitTags)
            .FirstOrDefaultAsync(habit => habit.Id == habitId);
        if (habit is null)
            return NotFound();

        // If the tag is already associated with the habit, do nothing
        if (habit.HabitTags.Any(habitTags => habitTags.TagId == tagId))
            return NoContent();

        // Check if the tag exists
        if (!await context.Tags.AnyAsync(tag => tag.Id == tagId))
            return NotFound();

        context.HabitTags.Add(new HabitTag { HabitId = habitId, TagId = tagId, CreatedDateTime = DateTime.UtcNow });
        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> RemoveTagFromHabit(string habitId, string tagId) {
        HabitTag? habitTag =
            await context.HabitTags.FirstOrDefaultAsync(habitTag =>
                habitTag.HabitId == habitId && habitTag.TagId == tagId);
        if (habitTag is null)
            return NotFound();

        context.HabitTags.Remove(habitTag);
        await context.SaveChangesAsync();
        return NoContent();
    }
}
