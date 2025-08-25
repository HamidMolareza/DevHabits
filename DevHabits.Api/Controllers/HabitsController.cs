using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DevHabits.Api.Database;
using DevHabits.Api.Dtos.Habits;
using DevHabits.Api.Entities;

namespace DevHabits.Api.Controllers;

[Route("habits")]
[ApiController]
public class HabitsController(ApplicationDbContext context) : ControllerBase {
    // GET: Habits
    [HttpGet]
    public async Task<ActionResult<HabitsCollectionResponse>> GetHabits() {
        List<HabitResponse> habitDtos = await context.Habits
            .Select(HabitQueries.ProjectToDto())
            .ToListAsync();
        return new HabitsCollectionResponse { Data = habitDtos };
    }

    // GET: Habits/5
    [HttpGet("{id}")]
    public async Task<ActionResult<HabitWithTagsResponse>> GetHabit(string id) {
        HabitWithTagsResponse? habit = await context.Habits
            .Where(habit => habit.Id == id)
            .Include(habit => habit.HabitTags)
            .ThenInclude(ht => ht.Tag)
            .Select(HabitQueries.ProjectToHabitWithTagsDto())
            .FirstOrDefaultAsync();

        if (habit == null)
            return NotFound();

        return habit;
    }

    // PUT: Habits/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutHabit(string id, UpdateHabitRequest habitRequest) {
        if (id != habitRequest.Id)
            return BadRequest();

        Habit? existingHabit = await context.Habits.FindAsync(id);
        if (existingHabit == null)
            return NotFound();

        context.Attach(existingHabit);

        existingHabit.UpdateFromDto(habitRequest);
        await context.SaveChangesAsync();

        return NoContent();
    }

    // POST: Habits
    [HttpPost]
    public async Task<ActionResult<HabitResponse>> PostHabit([FromBody] CreateHabitRequest habitRequest) {
        Habit habit = habitRequest.ToEntity();

        context.Habits.Add(habit);
        await context.SaveChangesAsync();

        return CreatedAtAction("GetHabit", new { id = habit.Id }, habit);
    }

    // DELETE: Habits/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteHabit(string id) {
        var entity = new Habit { Id = id };
        context.Habits.Remove(entity);
        try {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException) {
            if (!await context.Habits.AnyAsync(habit => habit.Id == id))
                return NotFound();
            throw;
        }

        return NoContent();
    }
}
